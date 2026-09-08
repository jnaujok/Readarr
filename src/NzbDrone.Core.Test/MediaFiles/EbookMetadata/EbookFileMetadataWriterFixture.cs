using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.EbookMetadata;
using NzbDrone.Core.Test.Framework;
using VersOne.Epub;

namespace NzbDrone.Core.Test.MediaFiles.EbookMetadata
{
    [TestFixture]
    public class EbookFileMetadataWriterFixture : CoreTest<EbookFileMetadataWriter>
    {
        [Test]
        public void can_write_epub_and_pdf()
        {
            Subject.CanWrite("a.epub").Should().BeTrue();
            Subject.CanWrite("a.kepub").Should().BeTrue();
            Subject.CanWrite("a.pdf").Should().BeTrue();
            Subject.CanWrite("a.mobi").Should().BeFalse();
        }

        [Test]
        public void write_throws_for_unsupported_extension()
        {
            Assert.Throws<NotSupportedException>(() =>
                Subject.Write("book.mobi", new EbookFileMetadata { Title = "X" }, false));
        }

        [Test]
        public void writes_epub_through_facade()
        {
            var path = GetTempFilePath() + ".epub";
            EpubFileMetadataWriterFixture.CreateMinimalEpub(path, "Old", "Anon");
            var metadata = new EbookFileMetadata { Title = "Via Facade" };

            Subject.Write(path, metadata, false);

            using (var book = EpubReader.OpenBook(path))
            {
                book.Title.Should().Be("Via Facade");
            }
        }

        [Test]
        public void cannot_write_empty_path()
        {
            Subject.CanWrite(null).Should().BeFalse();
            Subject.CanWrite("").Should().BeFalse();
        }
    }
}
