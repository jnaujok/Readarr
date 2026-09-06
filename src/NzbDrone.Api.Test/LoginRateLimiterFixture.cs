using FluentAssertions;
using NUnit.Framework;
using Readarr.Http.Authentication;

namespace NzbDrone.Api.Test
{
    [TestFixture]
    public class LoginRateLimiterFixture
    {
        [Test]
        public void blocks_after_max_failures()
        {
            var limiter = new LoginRateLimiter();
            var ip = "203.0.113.9";

            for (var i = 0; i < LoginRateLimiter.MaxFailures; i++)
            {
                limiter.IsBlocked(ip).Should().BeFalse();
                limiter.RecordFailure(ip);
            }

            limiter.IsBlocked(ip).Should().BeTrue();
        }

        [Test]
        public void success_clears_failures()
        {
            var limiter = new LoginRateLimiter();
            var ip = "203.0.113.10";

            for (var i = 0; i < LoginRateLimiter.MaxFailures; i++)
            {
                limiter.RecordFailure(ip);
            }

            limiter.RecordSuccess(ip);
            limiter.IsBlocked(ip).Should().BeFalse();
        }
    }
}
