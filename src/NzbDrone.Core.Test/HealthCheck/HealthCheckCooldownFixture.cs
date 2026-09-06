using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.HealthCheck;

namespace NzbDrone.Core.Test.HealthCheck
{
    [TestFixture]
    public class HealthCheckCooldownFixture
    {
        [Test]
        public void first_call_is_allowed()
        {
            var cooldown = new HealthCheckCooldown(TimeSpan.FromSeconds(15));

            cooldown.TryEnter(DateTime.UtcNow).Should().BeTrue();
        }

        [Test]
        public void second_call_within_interval_is_blocked()
        {
            var cooldown = new HealthCheckCooldown(TimeSpan.FromSeconds(15));
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            cooldown.TryEnter(now).Should().BeTrue();
            cooldown.TryEnter(now.AddSeconds(5)).Should().BeFalse();
        }

        [Test]
        public void call_after_interval_is_allowed()
        {
            var cooldown = new HealthCheckCooldown(TimeSpan.FromSeconds(15));
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            cooldown.TryEnter(now).Should().BeTrue();
            cooldown.TryEnter(now.AddSeconds(15)).Should().BeTrue();
        }
    }
}
