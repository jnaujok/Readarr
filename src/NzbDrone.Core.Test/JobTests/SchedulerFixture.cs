using System;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Jobs;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.JobTests
{
    [TestFixture]
    public class SchedulerFixture : CoreTest<Scheduler>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<ITaskManager>()
                  .Setup(s => s.GetPending())
                  .Returns(Array.Empty<ScheduledTask>());
        }

        [Test]
        public void start_is_idempotent_and_shutdown_unsubscribes()
        {
            Subject.Handle(new ApplicationStartedEvent());
            Subject.Handle(new ApplicationStartedEvent());
            Subject.Handle(new ApplicationShutdownRequested());
            Subject.Handle(new ApplicationShutdownRequested());

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(s => s.Push(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CommandPriority>(), It.IsAny<CommandTrigger>()), Times.Never());
        }
    }
}
