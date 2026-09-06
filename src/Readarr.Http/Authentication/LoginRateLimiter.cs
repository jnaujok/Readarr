using System;
using System.Collections.Concurrent;
using NzbDrone.Common.Extensions;

namespace Readarr.Http.Authentication
{
    public class LoginRateLimiter
    {
        public const int MaxFailures = 5;
        public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

        private readonly ConcurrentDictionary<string, FailureState> _failures = new ConcurrentDictionary<string, FailureState>();

        public bool IsBlocked(string ip)
        {
            if (ip.IsNullOrWhiteSpace())
            {
                return false;
            }

            if (!_failures.TryGetValue(ip, out var state))
            {
                return false;
            }

            if (DateTime.UtcNow - state.FirstUtc > Window)
            {
                _failures.TryRemove(ip, out _);
                return false;
            }

            return state.Count >= MaxFailures;
        }

        public void RecordFailure(string ip)
        {
            if (ip.IsNullOrWhiteSpace())
            {
                return;
            }

            _failures.AddOrUpdate(
                ip,
                _ => new FailureState { Count = 1, FirstUtc = DateTime.UtcNow },
                (_, existing) =>
                {
                    if (DateTime.UtcNow - existing.FirstUtc > Window)
                    {
                        return new FailureState { Count = 1, FirstUtc = DateTime.UtcNow };
                    }

                    existing.Count++;
                    return existing;
                });
        }

        public void RecordSuccess(string ip)
        {
            if (ip.IsNullOrWhiteSpace())
            {
                return;
            }

            _failures.TryRemove(ip, out _);
        }

        private sealed class FailureState
        {
            public int Count { get; set; }
            public DateTime FirstUtc { get; set; }
        }
    }
}
