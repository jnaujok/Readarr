using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Housekeeping;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping
{
    [TestFixture]
    public class HousekeepingServiceFixture : CoreTest<HousekeepingService>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<IEnumerable<IHousekeepingTask>>(new List<IHousekeepingTask>());
        }

        [Test]
        public void vacuums_sqlite_once_then_skips()
        {
            Mocker.GetMock<IMainDatabase>()
                  .SetupGet(s => s.DatabaseType)
                  .Returns(DatabaseType.SQLite);

            Subject.Execute(new HousekeepingCommand());
            Subject.Execute(new HousekeepingCommand());

            Mocker.GetMock<IMainDatabase>().Verify(v => v.Vacuum(), Times.Once());
        }

        [Test]
        public void does_not_vacuum_postgres()
        {
            Mocker.GetMock<IMainDatabase>()
                  .SetupGet(s => s.DatabaseType)
                  .Returns(DatabaseType.PostgreSQL);

            Subject.Execute(new HousekeepingCommand());

            Mocker.GetMock<IMainDatabase>().Verify(v => v.Vacuum(), Times.Never());
        }
    }
}
