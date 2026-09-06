using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Books
{
    public static class BookFormatPreference
    {
        public static BookFormatKind GetKind(Quality quality)
        {
            if (quality == null)
            {
                return BookFormatKind.Ebook;
            }

            return quality.FormatKind;
        }

        public static BookFormatKind GetKind(BookFile file)
        {
            return GetKind(file?.Quality?.Quality);
        }

        public static List<BookFormatKind> GetWantedKinds(Book book, QualityProfile profile)
        {
            if (book?.WantedFormatKinds != null)
            {
                return book.WantedFormatKinds.Distinct().ToList();
            }

            if (profile?.WantedFormatKinds == null)
            {
                return new List<BookFormatKind>();
            }

            return profile.WantedFormatKinds.Distinct().ToList();
        }

        public static HashSet<BookFormatKind> GetCollectedKinds(IEnumerable<BookFile> files)
        {
            return (files ?? Enumerable.Empty<BookFile>())
                .Where(f => f != null)
                .Select(GetKind)
                .ToHashSet();
        }

        public static List<BookFormatKind> GetMissingKinds(Book book, QualityProfile profile, IEnumerable<BookFile> files)
        {
            var wanted = GetWantedKinds(book, profile);
            var collected = GetCollectedKinds(files);

            if (wanted.Count == 0)
            {
                return new List<BookFormatKind>();
            }

            return wanted.Where(w => !collected.Contains(w)).ToList();
        }

        public static bool IsMissing(Book book, QualityProfile profile, IEnumerable<BookFile> files)
        {
            var fileList = (files ?? Enumerable.Empty<BookFile>()).Where(f => f != null).ToList();
            var wanted = GetWantedKinds(book, profile);

            if (wanted.Count == 0)
            {
                return fileList.Count == 0;
            }

            return GetMissingKinds(book, profile, fileList).Count > 0;
        }
    }
}
