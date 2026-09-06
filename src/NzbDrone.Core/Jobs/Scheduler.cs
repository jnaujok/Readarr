using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using NLog;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using Timer = System.Timers.Timer;

namespace NzbDrone.Core.Jobs
{
    public class Scheduler :
        IHandle<ApplicationStartedEvent>,
        IHandle<ApplicationShutdownRequested>
    {
        private readonly ITaskManager _taskManager;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;
        private readonly object _mutex = new object();
        private readonly Timer _timer = new Timer();

        private CancellationTokenSource _cancellationTokenSource;
        private ElapsedEventHandler _elapsedHandler;
        private volatile bool _stopped = true;

        public Scheduler(ITaskManager taskManager, IManageCommandQueue commandQueueManager, Logger logger)
        {
            _taskManager = taskManager;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        private void ExecuteCommands()
        {
            try
            {
                _timer.Enabled = false;

                var tasks = _taskManager.GetPending().ToList();

                _logger.Trace("Pending Tasks: {0}", tasks.Count);

                foreach (var task in tasks)
                {
                    _commandQueueManager.Push(task.TypeName, task.LastExecution, task.LastStartTime, task.Priority, CommandTrigger.Scheduled);
                }
            }
            finally
            {
                if (!_stopped)
                {
                    _timer.Enabled = true;
                }
            }
        }

        public void Handle(ApplicationStartedEvent message)
        {
            lock (_mutex)
            {
                if (_cancellationTokenSource != null)
                {
                    return;
                }

                _stopped = false;
                _cancellationTokenSource = new CancellationTokenSource();
                _timer.Interval = 1000 * 30;
                _elapsedHandler = (o, args) => Task.Factory.StartNew(ExecuteCommands, _cancellationTokenSource.Token)
                    .LogExceptions();
                _timer.Elapsed += _elapsedHandler;
                _timer.Start();
            }
        }

        public void Handle(ApplicationShutdownRequested message)
        {
            _logger.Info("Shutting down scheduler");

            lock (_mutex)
            {
                _stopped = true;

                try
                {
                    _cancellationTokenSource?.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }

                _timer.Stop();

                if (_elapsedHandler != null)
                {
                    _timer.Elapsed -= _elapsedHandler;
                    _elapsedHandler = null;
                }

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }
}
