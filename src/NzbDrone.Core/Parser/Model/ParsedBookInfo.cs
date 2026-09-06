using System.Collections.Generic;
using System.Text.Json.Serialization;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public class ParsedBookInfo
    {
        public string BookTitle { get; set; }
        public string AuthorName { get; set; }
        public AuthorTitleInfo AuthorTitleInfo { get; set; }
        public QualityModel Quality { get; set; }
        public List<Quality> Qualities { get; set; } = new List<Quality>();
        public string ReleaseDate { get; set; }
        public bool Discography { get; set; }
        public int DiscographyStart { get; set; }
        public int DiscographyEnd { get; set; }
        public string ReleaseGroup { get; set; }
        public string ReleaseHash { get; set; }
        public string ReleaseVersion { get; set; }
        public string ReleaseTitle { get; set; }

        [JsonIgnore]
        public Dictionary<string, object> ExtraInfo { get; set; } = new Dictionary<string, object>();

        public void ApplyQuality(string title, string desc = null, List<int> categories = null)
        {
            var parsed = QualityParser.ParseQuality(title, desc, categories);
            var extras = QualityParser.ParseQualities(title);

            if (parsed.Quality != NzbDrone.Core.Qualities.Quality.Unknown && !extras.Contains(parsed.Quality))
            {
                extras.Insert(0, parsed.Quality);
            }

            Quality = parsed;
            Qualities = extras;
        }

        public override string ToString()
        {
            var bookString = "[Unknown Book]";

            if (BookTitle != null)
            {
                bookString = string.Format("{0}", BookTitle);
            }

            return string.Format("{0} - {1} {2}", AuthorName, bookString, Quality);
        }
    }
}
