using System;
using System.IO;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.MediaFiles.EbookMetadata
{
    public class EbookFileMetadataWriter : IEbookFileMetadataWriter
    {
        private readonly EpubFileMetadataWriter _epub;
        private readonly PdfFileMetadataWriter _pdf;

        public EbookFileMetadataWriter()
        {
            _epub = new EpubFileMetadataWriter();
            _pdf = new PdfFileMetadataWriter();
        }

        public bool CanWrite(string path)
        {
            if (path.IsNullOrWhiteSpace())
            {
                return false;
            }

            return _epub.CanWrite(path) || _pdf.CanWrite(path);
        }

        public void Write(string path, EbookFileMetadata metadata, bool updateCover)
        {
            if (_epub.CanWrite(path))
            {
                _epub.Write(path, metadata, updateCover);
                return;
            }

            if (_pdf.CanWrite(path))
            {
                _pdf.Write(path, metadata, updateCover);
                return;
            }

            throw new NotSupportedException($"No ebook metadata writer for '{Path.GetExtension(path)}'.");
        }
    }
}
