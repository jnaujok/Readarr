using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books.Calibre;

namespace NzbDrone.Core.Test.CalibreTests
{
    [TestFixture]
    public class CalibreConversionPollerFixture
    {
        [Test]
        public async Task returns_when_conversion_completes()
        {
            var polls = 0;
            var failed = false;

            await CalibreConversionPoller.PollAsync(
                () =>
                {
                    polls++;
                    return new CalibreConversionStatus { Running = false, Ok = true };
                },
                (trace, log) => failed = true,
                message => failed = true,
                5,
                CancellationToken.None,
                (_, _) => Task.CompletedTask);

            polls.Should().Be(1);
            failed.Should().BeFalse();
        }

        [Test]
        public async Task reports_failure_when_conversion_not_ok()
        {
            string traceback = null;
            string log = null;

            await CalibreConversionPoller.PollAsync(
                () => new CalibreConversionStatus { Running = false, Ok = false, Traceback = "boom", Log = "log" },
                (trace, statusLog) =>
                {
                    traceback = trace;
                    log = statusLog;
                },
                _ => { },
                5,
                CancellationToken.None,
                (_, _) => Task.CompletedTask);

            traceback.Should().Be("boom");
            log.Should().Be("log");
        }

        [Test]
        public async Task times_out_after_max_attempts()
        {
            var polls = 0;
            string timeout = null;

            await CalibreConversionPoller.PollAsync(
                () =>
                {
                    polls++;
                    return new CalibreConversionStatus { Running = true, Ok = true };
                },
                (_, _) => { },
                message => timeout = message,
                3,
                CancellationToken.None,
                (_, _) => Task.CompletedTask);

            polls.Should().Be(3);
            timeout.Should().Contain("3");
        }

        [Test]
        public void rejects_non_positive_max_attempts()
        {
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => CalibreConversionPoller.PollAsync(
                    () => new CalibreConversionStatus { Running = true, Ok = true },
                    (_, _) => { },
                    _ => { },
                    0,
                    CancellationToken.None,
                    (_, _) => Task.CompletedTask));
        }

        [Test]
        public void cancels_without_spinning()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.ThrowsAsync<OperationCanceledException>(
                () => CalibreConversionPoller.PollAsync(
                    () => new CalibreConversionStatus { Running = true, Ok = true },
                    (_, _) => { },
                    _ => { },
                    50,
                    cts.Token,
                    (_, _) => Task.CompletedTask));
        }
    }
}
