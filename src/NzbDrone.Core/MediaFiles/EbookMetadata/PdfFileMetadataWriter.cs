using System;
using System.IO;
using System.Linq;
using NzbDrone.Common.Extensions;
using PdfSharpCore.Pdf.IO;

namespace NzbDrone.Core.MediaFiles.EbookMetadata
{
    public class PdfFileMetadataWriter
    {
        public bool CanWrite(string path)
        {
            return Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        public void Write(string path, EbookFileMetadata metadata, bool updateCover)
        {
            if (path.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Path is required", nameof(path));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("PDF file not found.", path);
            }

            var tempPath = path + ".tmpwrite";

            try
            {
                using (var document = PdfReader.Open(path, PdfDocumentOpenMode.Modify))
                {
                    if (document.Info != null)
                    {
                        if (metadata.Title.IsNotNullOrWhiteSpace())
                        {
                            document.Info.Title = metadata.Title;
                        }

                        if (metadata.Authors != null && metadata.Authors.Any(a => a.IsNotNullOrWhiteSpace()))
                        {
                            document.Info.Author = string.Join("; ", metadata.Authors.Where(a => a.IsNotNullOrWhiteSpace()));
                        }

                        if (metadata.Publisher.IsNotNullOrWhiteSpace())
                        {
                            document.Info.Creator = metadata.Publisher;
                        }
                    }

                    document.Save(tempPath);
                }

                File.Move(tempPath, path, true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
    }
}
