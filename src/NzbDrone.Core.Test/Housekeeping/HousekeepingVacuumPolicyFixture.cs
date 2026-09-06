using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Housekeeping;

namespace NzbDrone.Core.Test.Housekeeping
{
    [TestFixture]
    public class HousekeepingVacuumPolicyFixture
    {
        [Test]
        public void vacuums_sqlite_when_never_run()
        {
            HousekeepingVacuumPolicy.ShouldVacuum(DatabaseType.SQLite, null, DateTime.UtcNow).Should().BeTrue();
        }

        [Test]
        public void skips_postgres()
        {
            HousekeepingVacuumPolicy.ShouldVacuum(DatabaseType.PostgreSQL, null, DateTime.UtcNow).Should().BeFalse();
        }

        [Test]
        public void skips_sqlite_inside_interval()
        {
            var last = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            HousekeepingVacuumPolicy.ShouldVacuum(DatabaseType.SQLite, last, last.AddDays(1)).Should().BeFalse();
        }

        [Test]
        public void vacuums_sqlite_after_interval()
        {
            var last = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            HousekeepingVacuumPolicy.ShouldVacuum(DatabaseType.SQLite, last, last.Add(HousekeepingVacuumPolicy.Interval)).Should().BeTrue();
        }
    }
}
