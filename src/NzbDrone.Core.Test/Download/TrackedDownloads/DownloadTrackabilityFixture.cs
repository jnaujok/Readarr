using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.TrackedDownloads
{
    [TestFixture]
    public class DownloadTrackabilityFixture : CoreTest
    {
        private static TrackedDownload Given(TrackedDownloadState state, DownloadItemStatus status)
        {
            return new TrackedDownload
            {
                State = state,
                DownloadItem = new DownloadClientItem
                {
                    Status = status,
                    Title = "Some Book"
                }
            };
        }

        [Test]
        public void should_keep_completed_download_in_queue_when_cdh_is_disabled()
        {
            var tracked = Given(TrackedDownloadState.ImportPending, DownloadItemStatus.Completed);

            DownloadTrackability.ShouldKeepInQueue(tracked).Should().BeTrue();
        }

        [Test]
        public void should_keep_downloading_items()
        {
            var tracked = Given(TrackedDownloadState.Downloading, DownloadItemStatus.Downloading);

            DownloadTrackability.ShouldKeepInQueue(tracked).Should().BeTrue();
        }

        [Test]
        public void should_drop_imported_failed_and_ignored()
        {
            DownloadTrackability.ShouldKeepInQueue(Given(TrackedDownloadState.Imported, DownloadItemStatus.Completed)).Should().BeFalse();
            DownloadTrackability.ShouldKeepInQueue(Given(TrackedDownloadState.DownloadFailed, DownloadItemStatus.Failed)).Should().BeFalse();
            DownloadTrackability.ShouldKeepInQueue(Given(TrackedDownloadState.Ignored, DownloadItemStatus.Completed)).Should().BeFalse();
        }

        [Test]
        public void should_drop_null_items()
        {
            DownloadTrackability.ShouldKeepInQueue(null).Should().BeFalse();
            DownloadTrackability.ShouldKeepInQueue(new TrackedDownload()).Should().BeFalse();
        }
    }
}
