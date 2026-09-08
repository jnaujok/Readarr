using System;
using System.Collections.Generic;

namespace NzbDrone.Core.MediaFiles.EbookMetadata
{
    public class EbookFileMetadata
    {
        public string Title { get; set; }
        public List<string> Authors { get; set; } = new List<string>();
        public string Publisher { get; set; }
        public string Language { get; set; }
        public string Description { get; set; }
        public string Isbn { get; set; }
        public string Asin { get; set; }
        public string Series { get; set; }
        public string SeriesIndex { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public byte[] Cover { get; set; }
        public string CoverExtension { get; set; }
    }
}
