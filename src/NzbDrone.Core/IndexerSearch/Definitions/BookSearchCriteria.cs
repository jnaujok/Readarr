using System;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class BookSearchCriteria : SearchCriteriaBase
    {
        public string BookTitle { get; set; }
        public int BookYear { get; set; }
        public string BookIsbn { get; set; }
        public string Disambiguation { get; set; }

        public string BookQuery
        {
            get
            {
                var title = BookTitle ?? string.Empty;
                var authorPrefix = $"{Author.Name}:";

                if (title.StartsWith(authorPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    title = title.Substring(authorPrefix.Length).Trim();
                }

                return GetQueryTitle(title);
            }
        }

        public override string ToString()
        {
            return $"[{Author.Name} - {BookTitle}]";
        }
    }
}
