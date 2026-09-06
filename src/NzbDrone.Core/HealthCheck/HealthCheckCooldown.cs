using System;

namespace NzbDrone.Core.HealthCheck
{
    public class HealthCheckCooldown
    {
        private readonly TimeSpan _interval;
        private DateTime _lastRun = DateTime.MinValue;

        public HealthCheckCooldown(TimeSpan interval)
        {
            _interval = interval;
        }

        public bool TryEnter(DateTime nowUtc)
        {
            if (nowUtc - _lastRun < _interval)
            {
                return false;
            }

            _lastRun = nowUtc;
            return true;
        }
    }
}
