using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class BookFormatPreferenceFixture : CoreTest
    {
        private static BookFile File(Quality quality)
        {
            return new BookFile { Quality = new QualityModel(quality) };
        }

        [Test]
        public void quality_format_kind_maps_pdf_ebook_and_audio()
        {
            Quality.PDF.FormatKind.Should().Be(BookFormatKind.Pdf);
            Quality.EPUB.FormatKind.Should().Be(BookFormatKind.Ebook);
            Quality.AZW3.FormatKind.Should().Be(BookFormatKind.Ebook);
            Quality.MOBI.FormatKind.Should().Be(BookFormatKind.Ebook);
            Quality.MP3.FormatKind.Should().Be(BookFormatKind.Audiobook);
            Quality.M4B.FormatKind.Should().Be(BookFormatKind.Audiobook);
            Quality.FLAC.FormatKind.Should().Be(BookFormatKind.Audiobook);
        }

        [Test]
        public void inherits_profile_wanted_kinds_when_book_has_none()
        {
            var book = new Book();
            var profile = new QualityProfile
            {
                WantedFormatKinds = new List<BookFormatKind> { BookFormatKind.Ebook, BookFormatKind.Audiobook }
            };

            BookFormatPreference.GetWantedKinds(book, profile)
                .Should().Equal(BookFormatKind.Ebook, BookFormatKind.Audiobook);
        }

        [Test]
        public void book_override_replaces_profile()
        {
            var book = new Book
            {
                WantedFormatKinds = new List<BookFormatKind> { BookFormatKind.Pdf }
            };
            var profile = new QualityProfile
            {
                WantedFormatKinds = new List<BookFormatKind> { BookFormatKind.Ebook, BookFormatKind.Audiobook }
            };

            BookFormatPreference.GetWantedKinds(book, profile)
                .Should().Equal(BookFormatKind.Pdf);
        }

        [Test]
        public void empty_wanted_kinds_is_missing_only_without_files()
        {
            var book = new Book();
            var profile = new QualityProfile { WantedFormatKinds = new List<BookFormatKind>() };

            BookFormatPreference.IsMissing(book, profile, new List<BookFile>()).Should().BeTrue();
            BookFormatPreference.IsMissing(book, profile, new List<BookFile> { File(Quality.EPUB) }).Should().BeFalse();
        }

        [Test]
        public void epub_does_not_satisfy_pdf_or_audiobook()
        {
            var book = new Book();
            var profile = new QualityProfile
            {
                WantedFormatKinds = new List<BookFormatKind>
                {
                    BookFormatKind.Ebook,
                    BookFormatKind.Pdf,
                    BookFormatKind.Audiobook
                }
            };

            var missing = BookFormatPreference.GetMissingKinds(book, profile, new List<BookFile> { File(Quality.EPUB) });

            missing.Should().BeEquivalentTo(BookFormatKind.Pdf, BookFormatKind.Audiobook);
            BookFormatPreference.IsMissing(book, profile, new List<BookFile> { File(Quality.EPUB) }).Should().BeTrue();
        }

        [Test]
        public void collected_kinds_include_each_present_format()
        {
            var files = new List<BookFile>
            {
                File(Quality.EPUB),
                File(Quality.PDF),
                File(Quality.MP3)
            };

            BookFormatPreference.GetCollectedKinds(files)
                .Should().BeEquivalentTo(BookFormatKind.Ebook, BookFormatKind.Pdf, BookFormatKind.Audiobook);

            var book = new Book();
            var profile = new QualityProfile
            {
                WantedFormatKinds = new List<BookFormatKind>
                {
                    BookFormatKind.Ebook,
                    BookFormatKind.Pdf,
                    BookFormatKind.Audiobook
                }
            };

            BookFormatPreference.IsMissing(book, profile, files).Should().BeFalse();
        }
    }
}
