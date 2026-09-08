using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Calibre;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.EbookMetadata;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using VersOne.Epub.Schema;

namespace NzbDrone.Core.Test.MediaFiles.AudioTagServiceFixture
{
    [TestFixture]
    public class EbookTagServiceFixture : CoreTest<EBookTagService>
    {
        private BookFile _file;

        [SetUp]
        public void Setup()
        {
            var author = Builder<Author>.CreateNew().With(a => a.Name = "Jane Austen").Build();
            var book = Builder<Book>.CreateNew()
                .With(b => b.Author = author)
                .With(b => b.SeriesLinks = new List<SeriesBookLink>())
                .Build();
            var edition = Builder<Edition>.CreateNew()
                .With(e => e.Title = "Pride and Prejudice")
                .With(e => e.Isbn13 = "9781455546176")
                .With(e => e.Book = book)
                .Build();

            _file = Builder<BookFile>.CreateNew()
                .With(f => f.Path = "/library/book.epub")
                .With(f => f.CalibreId = 0)
                .With(f => f.Edition = edition)
                .With(f => f.Author = author)
                .Build();

            Mocker.GetMock<IConfigService>().SetupGet(c => c.WriteBookTags).Returns(WriteBookTagsType.AllFiles);

            var fileInfo = new Mock<IFileInfo>();
            fileInfo.SetupGet(f => f.Length).Returns(1234);
            fileInfo.SetupGet(f => f.LastWriteTimeUtc).Returns(DateTime.UtcNow);
            Mocker.GetMock<IDiskProvider>().Setup(d => d.GetFileInfo(_file.Path)).Returns(fileInfo.Object);
            Mocker.GetMock<IEbookFileMetadataWriter>().Setup(w => w.CanWrite(_file.Path)).Returns(true);
        }

        [Test]
        public void should_prefer_isbn13()
        {
            var ids = Builder<EpubMetadataIdentifier>
                .CreateListOfSize(2)
                .TheFirst(1)
                .With(x => x.Identifier = "4087738574")
                .TheNext(1)
                .With(x => x.Identifier = "9781455546176")
                .Build()
                .ToList();

            Subject.GetIsbn(ids).Should().Be("9781455546176");
        }

        [Test]
        public void writes_local_epub_when_not_a_calibre_library()
        {
            Mocker.GetMock<IRootFolderService>()
                .Setup(s => s.GetBestRootFolder(_file.Path))
                .Returns(new RootFolder { IsCalibreLibrary = false });

            Subject.WriteTags(_file, true);

            Mocker.GetMock<IEbookFileMetadataWriter>()
                .Verify(v => v.Write(_file.Path, It.Is<EbookFileMetadata>(m => m.Title == "Pride and Prejudice" && m.Isbn == "9781455546176"), It.IsAny<bool>()), Times.Once());

            Mocker.GetMock<ICalibreProxy>()
                .Verify(v => v.SetFields(It.IsAny<BookFile>(), It.IsAny<CalibreSettings>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void uses_calibre_when_library_is_calibre()
        {
            _file.CalibreId = 12;
            var settings = new CalibreSettings { Host = "127.0.0.1" };

            Mocker.GetMock<IRootFolderService>()
                .Setup(s => s.GetBestRootFolder(_file.Path))
                .Returns(new RootFolder { IsCalibreLibrary = true, CalibreSettings = settings });

            Subject.WriteTags(_file, true);

            Mocker.GetMock<ICalibreProxy>()
                .Verify(v => v.SetFields(_file, settings, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once());

            Mocker.GetMock<IEbookFileMetadataWriter>()
                .Verify(v => v.Write(It.IsAny<string>(), It.IsAny<EbookFileMetadata>(), It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void diff_metadata_is_empty_when_tags_match()
        {
            var current = new ParsedTrackInfo
            {
                BookTitle = "Pride and Prejudice",
                Authors = new List<string> { "Jane Austen" },
                Isbn = "9781455546176"
            };
            var desired = new EbookFileMetadata
            {
                Title = "Pride and Prejudice",
                Authors = { "Jane Austen" },
                Isbn = "9781455546176"
            };

            EBookTagService.DiffMetadata(current, desired).Should().BeEmpty();
        }

        [Test]
        public void diff_metadata_reports_title_change()
        {
            var current = new ParsedTrackInfo { BookTitle = "Old", Authors = new List<string> { "A" } };
            var desired = new EbookFileMetadata { Title = "New", Authors = { "A" } };

            var diff = EBookTagService.DiffMetadata(current, desired);
            diff.Should().ContainKey("Title");
            diff["Title"].Item1.Should().Be("Old");
            diff["Title"].Item2.Should().Be("New");
        }
    }
}
