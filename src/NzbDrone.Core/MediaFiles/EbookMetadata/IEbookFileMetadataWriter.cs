namespace NzbDrone.Core.MediaFiles.EbookMetadata
{
    public interface IEbookFileMetadataWriter
    {
        bool CanWrite(string path);
        void Write(string path, EbookFileMetadata metadata, bool updateCover);
    }
}
