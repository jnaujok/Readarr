using System;
using System.Threading;
using System.Threading.Tasks;

namespace NzbDrone.Core.Books.Calibre
{
    public static class CalibreConversionPoller
    {
        public const int DefaultMaxAttempts = 900;
        public const int DefaultDelayMilliseconds = 2000;

        public static async Task PollAsync(
            Func<CalibreConversionStatus> getStatus,
            Action<string, string> onFailed,
            Action<string> onTimeout,
            int maxAttempts,
            CancellationToken cancellationToken,
            Func<int, CancellationToken, Task> delay)
        {
            if (maxAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts));
            }

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var status = getStatus();

                if (!status.Running)
                {
                    if (!status.Ok)
                    {
                        onFailed(status.Traceback, status.Log);
                    }

                    return;
                }

                await delay(DefaultDelayMilliseconds, cancellationToken);
            }

            onTimeout($"Conversion still running after {maxAttempts} polls");
        }
    }
}
