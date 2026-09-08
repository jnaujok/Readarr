using System.IO;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.EbookMetadata;
using NzbDrone.Core.Test.Framework;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace NzbDrone.Core.Test.MediaFiles.EbookMetadata
{
    [TestFixture]
    public class PdfFileMetadataWriterFixture : CoreTest<PdfFileMetadataWriter>
    {
        [Test]
        public void can_write_pdf_only()
        {
            Subject.CanWrite("/lib/book.pdf").Should().BeTrue();
            Subject.CanWrite("/lib/book.epub").Should().BeFalse();
        }

        [Test]
        public void write_throws_when_file_missing()
        {
            Assert.Throws<FileNotFoundException>(() =>
                Subject.Write(GetTempFilePath() + ".pdf", new EbookFileMetadata { Title = "X" }, false));
        }

        [Test]
        public void writes_title_and_author()
        {
            var path = GetTempFilePath() + ".pdf";
            using (var document = new PdfDocument())
            {
                document.AddPage();
                document.Info.Title = "Old";
                document.Info.Author = "Anon";
                document.Save(path);
            }

            var metadata = new EbookFileMetadata
            {
                Title = "Pride and Prejudice",
                Authors = { "Jane Austen" },
                Publisher = "T. Egerton"
            };

            Subject.Write(path, metadata, false);

            using (var document = PdfReader.Open(path, PdfDocumentOpenMode.InformationOnly))
            {
                document.Info.Title.Should().Be("Pride and Prejudice");
                document.Info.Author.Should().Be("Jane Austen");
                document.Info.Creator.Should().Be("T. Egerton");
            }
        }
    }
}
