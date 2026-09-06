using System;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping
{
    public static class HousekeepingVacuumPolicy
    {
        public static readonly TimeSpan Interval = TimeSpan.FromDays(7);

        public static bool ShouldVacuum(DatabaseType databaseType, DateTime? lastVacuumUtc, DateTime nowUtc)
        {
            if (databaseType != DatabaseType.SQLite)
            {
                return false;
            }

            if (!lastVacuumUtc.HasValue)
            {
                return true;
            }

            return nowUtc - lastVacuumUtc.Value >= Interval;
        }
    }
}
