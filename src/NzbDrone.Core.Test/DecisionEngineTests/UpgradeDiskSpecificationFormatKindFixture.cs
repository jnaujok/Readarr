using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class UpgradeDiskSpecificationFormatKindFixture : CoreTest<UpgradeDiskSpecification>
    {
        private RemoteBook _remoteBook;
        private BookFile _ebookFile;

        [SetUp]
        public void Setup()
        {
            Mocker.Resolve<UpgradableSpecification>();

            _ebookFile = new BookFile { Quality = new QualityModel(Quality.EPUB) };

            var profile = new QualityProfile
            {
                UpgradeAllowed = true,
                Cutoff = Quality.AZW3.Id,
                Items = Qualities.QualityFixture.GetDefaultQualities(),
                WantedFormatKinds = new List<BookFormatKind> { BookFormatKind.Ebook, BookFormatKind.Audiobook }
            };

            var author = Builder<Author>
                .CreateNew()
                .With(a => a.QualityProfile = profile)
                .Build();

            _remoteBook = new RemoteBook
            {
                Author = author,
                ParsedBookInfo = new ParsedBookInfo { Quality = new QualityModel(Quality.MP3) },
                Books = new List<Book>
                {
                    new Book { BookFiles = new List<BookFile> { _ebookFile } }
                },
                CustomFormats = new List<CustomFormat>()
            };

            Mocker.GetMock<ICustomFormatCalculationService>()
                  .Setup(s => s.ParseCustomFormat(It.IsAny<BookFile>()))
                  .Returns(new List<CustomFormat>());
        }

        [Test]
        public void should_accept_audiobook_when_only_ebook_exists()
        {
            Subject.IsSatisfiedBy(_remoteBook, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_pdf_when_only_ebook_exists()
        {
            _remoteBook.ParsedBookInfo.Quality = new QualityModel(Quality.PDF);

            Subject.IsSatisfiedBy(_remoteBook, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_equal_ebook_when_ebook_already_on_disk()
        {
            _remoteBook.ParsedBookInfo.Quality = new QualityModel(Quality.EPUB);

            Subject.IsSatisfiedBy(_remoteBook, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_when_book_has_no_files()
        {
            _remoteBook.Books[0].BookFiles = new List<BookFile>();

            Subject.IsSatisfiedBy(_remoteBook, null).Accepted.Should().BeTrue();
        }
    }
}
