using System;
using System.Collections.Generic;
using System.Threading;
using NLog;
using NzbDrone.Common;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ProgressMessaging;

namespace NzbDrone.Core.Messaging.Commands
{
    public class CommandExecutor : IHandle<ApplicationStartedEvent>,
                                   IHandle<ApplicationShutdownRequested>
    {
        private const int THREAD_LIMIT = 3;
        private static readonly TimeSpan ShutdownJoinTimeout = TimeSpan.FromSeconds(15);

        private readonly Logger _logger;
        private readonly IServiceFactory _serviceFactory;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly object _workerLock = new object();
        private readonly List<Thread> _workers = new List<Thread>();

        private CancellationTokenSource _cancellationTokenSource;

        public CommandExecutor(IServiceFactory serviceFactory,
                               IManageCommandQueue commandQueueManager,
                               IEventAggregator eventAggregator,
                               Logger logger)
        {
            _logger = logger;
            _serviceFactory = serviceFactory;
            _commandQueueManager = commandQueueManager;
            _eventAggregator = eventAggregator;
        }

        private void ExecuteCommands()
        {
            CancellationToken token;

            lock (_workerLock)
            {
                if (_cancellationTokenSource == null)
                {
                    return;
                }

                token = _cancellationTokenSource.Token;
            }

            try
            {
                foreach (var command in _commandQueueManager.Queue(token))
                {
                    try
                    {
                        ExecuteCommand((dynamic)command.Body, command);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error occurred while executing task {0}", command.Name);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Trace("Stopped one command execution pipeline");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unknown error in thread");
            }
        }

        private void ExecuteCommand<TCommand>(TCommand command, CommandModel commandModel)
            where TCommand : Command
        {
            IExecute<TCommand> handler = null;

            try
            {
                handler = (IExecute<TCommand>)_serviceFactory.Build(typeof(IExecute<TCommand>));

                _logger.Trace("{0} -> {1}", command.GetType().Name, handler.GetType().Name);

                _commandQueueManager.Start(commandModel);
                BroadcastCommandUpdate(commandModel);

                if (ProgressMessageContext.CommandModel == null)
                {
                    ProgressMessageContext.CommandModel = commandModel;
                }

                handler.Execute(command);

                _commandQueueManager.Complete(commandModel, command.CompletionMessage ?? commandModel.Message);
            }
            catch (CommandFailedException ex)
            {
                _commandQueueManager.SetMessage(commandModel, "Failed");
                _commandQueueManager.Fail(commandModel, ex.Message, ex);
                _logger.Error(ex, "Error occurred while executing task {0}", commandModel.Name);
            }
            catch (Exception ex)
            {
                _commandQueueManager.SetMessage(commandModel, "Failed");
                _commandQueueManager.Fail(commandModel, "Failed", ex);
                _logger.Error(ex, "Error occurred while executing task {0}", commandModel.Name);
            }
            finally
            {
                BroadcastCommandUpdate(commandModel);

                _eventAggregator.PublishEvent(new CommandExecutedEvent(commandModel));

                if (ProgressMessageContext.CommandModel == commandModel)
                {
                    ProgressMessageContext.CommandModel = null;
                }

                if (handler != null)
                {
                    _logger.Trace("{0} <- {1} [{2}]", command.GetType().Name, handler.GetType().Name, commandModel.Duration.ToString());
                }
            }
        }

        private void BroadcastCommandUpdate(CommandModel command)
        {
            if (command.Body.SendUpdatesToClient)
            {
                _eventAggregator.PublishEvent(new CommandUpdatedEvent(command));
            }
        }

        public void Handle(ApplicationStartedEvent message)
        {
            lock (_workerLock)
            {
                if (_cancellationTokenSource != null)
                {
                    return;
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _workers.Clear();

                for (var i = 0; i < THREAD_LIMIT; i++)
                {
                    var thread = new Thread(ExecuteCommands)
                    {
                        Name = "CommandExecutor-" + i,
                        IsBackground = true
                    };

                    thread.Start();
                    _workers.Add(thread);
                }
            }
        }

        public void Handle(ApplicationShutdownRequested message)
        {
            _logger.Info("Shutting down task execution");

            List<Thread> workers;
            CancellationTokenSource cts;

            lock (_workerLock)
            {
                cts = _cancellationTokenSource;
                workers = new List<Thread>(_workers);
            }

            if (cts == null)
            {
                return;
            }

            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            var remaining = ShutdownJoinTimeout;
            foreach (var thread in workers)
            {
                if (remaining <= TimeSpan.Zero)
                {
                    _logger.Warn("Command worker {0} did not stop within {1} seconds", thread.Name, ShutdownJoinTimeout.TotalSeconds);
                    continue;
                }

                var started = DateTime.UtcNow;
                if (!thread.Join(remaining))
                {
                    _logger.Warn("Command worker {0} did not stop within {1} seconds", thread.Name, ShutdownJoinTimeout.TotalSeconds);
                    remaining = TimeSpan.Zero;
                    continue;
                }

                remaining -= DateTime.UtcNow - started;
            }

            lock (_workerLock)
            {
                if (!ReferenceEquals(_cancellationTokenSource, cts))
                {
                    return;
                }

                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                _workers.Clear();
            }
        }
    }
}
