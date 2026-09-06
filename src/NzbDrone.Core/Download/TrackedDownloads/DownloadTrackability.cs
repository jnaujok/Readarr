namespace NzbDrone.Core.Download.TrackedDownloads
{
    public static class DownloadTrackability
    {
        public static bool ShouldKeepInQueue(TrackedDownload trackedDownload)
        {
            if (trackedDownload == null || trackedDownload.DownloadItem == null)
            {
                return false;
            }

            if (trackedDownload.State == TrackedDownloadState.Imported ||
                trackedDownload.State == TrackedDownloadState.DownloadFailed ||
                trackedDownload.State == TrackedDownloadState.Ignored)
            {
                return false;
            }

            return true;
        }
    }
}
