using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.EbookMetadata;
using NzbDrone.Core.Test.Framework;
using VersOne.Epub;

namespace NzbDrone.Core.Test.MediaFiles.EbookMetadata
{
    [TestFixture]
    public class EpubFileMetadataWriterFixture : CoreTest<EpubFileMetadataWriter>
    {
        [Test]
        public void can_write_epub_and_kepub()
        {
            Subject.CanWrite("/lib/book.epub").Should().BeTrue();
            Subject.CanWrite("/lib/book.kepub").Should().BeTrue();
            Subject.CanWrite("/lib/book.pdf").Should().BeFalse();
        }

        [Test]
        public void writes_title_author_isbn_and_series_into_opf()
        {
            var path = GetTempFilePath() + ".epub";
            CreateMinimalEpub(path, "Old Title", "Old Author");

            var metadata = new EbookFileMetadata
            {
                Title = "New Title",
                Authors = { "Jane Austen" },
                Isbn = "9781455546176",
                Asin = "B00EXAMPLE",
                Series = "Penguin Classics",
                SeriesIndex = "3",
                Language = "en",
                Publisher = "Penguin",
                Description = "A novel."
            };

            Subject.Write(path, metadata, false);

            using (var book = EpubReader.OpenBook(path))
            {
                book.Title.Should().Be("New Title");
                book.AuthorList.Should().Contain("Jane Austen");
                var meta = book.Schema.Package.Metadata;
                meta.Publishers.Should().Contain("Penguin");
                meta.Languages.Should().Contain("en");
                meta.Description.Should().Be("A novel.");
                meta.Identifiers.Should().Contain(x => x.Identifier == "9781455546176");
                meta.Identifiers.Should().Contain(x => x.Identifier == "B00EXAMPLE");
                meta.MetaItems.Should().Contain(x => x.Name == "calibre:series" && x.Content == "Penguin Classics");
                meta.MetaItems.Should().Contain(x => x.Name == "calibre:series_index" && x.Content == "3");
            }
        }

        [Test]
        public void writes_cover_image_when_requested()
        {
            var path = GetTempFilePath() + ".epub";
            CreateMinimalEpub(path, "Old Title", "Old Author");
            var jpeg = Convert.FromBase64String("/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////wAALCAABAAEBAREA/8QAFAABAAAAAAAAAAAAAAAAAAAAA//EABQQAQAAAAAAAAAAAAAAAAAAAAD/2gAIAQEAAQUCf//Z");

            var metadata = new EbookFileMetadata
            {
                Title = "Covered",
                Cover = jpeg,
                CoverExtension = ".jpg"
            };

            Subject.Write(path, metadata, true);

            using (var zip = ZipFile.OpenRead(path))
            {
                zip.GetEntry("OEBPS/cover.jpg").Should().NotBeNull();
                using (var stream = zip.GetEntry("OEBPS/content.opf").Open())
                {
                    var opf = XDocument.Load(stream);
                    var xml = opf.ToString();
                    xml.Should().Contain("cover-image");
                    xml.Should().Contain("cover.jpg");
                }
            }
        }

        [Test]
        public void replaces_existing_cover_on_second_write()
        {
            var path = GetTempFilePath() + ".epub";
            CreateMinimalEpub(path, "Old Title", "Old Author");
            var jpeg = Convert.FromBase64String("/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////wAALCAABAAEBAREA/8QAFAABAAAAAAAAAAAAAAAAAAAAA//EABQQAQAAAAAAAAAAAAAAAAAAAAD/2gAIAQEAAQUCf//Z");
            var first = new EbookFileMetadata { Title = "One", Cover = jpeg, CoverExtension = ".jpg" };
            Subject.Write(path, first, true);
            var second = new EbookFileMetadata { Title = "Two", Cover = jpeg, CoverExtension = ".png" };
            Subject.Write(path, second, true);

            using (var book = EpubReader.OpenBook(path))
            {
                book.Title.Should().Be("Two");
            }
        }

        [Test]
        public void write_throws_when_metadata_null()
        {
            var path = GetTempFilePath() + ".epub";
            CreateMinimalEpub(path, "Old", "Anon");
            Assert.Throws<ArgumentNullException>(() => Subject.Write(path, null, false));
        }

        [Test]
        public void write_throws_when_file_missing()
        {
            Assert.Throws<FileNotFoundException>(() =>
                Subject.Write(GetTempFilePath() + ".epub", new EbookFileMetadata { Title = "X" }, false));
        }

        [Test]
        public void apply_metadata_replaces_title_on_document()
        {
            var opf = XDocument.Parse(@"<package xmlns=""http://www.idpf.org/2007/opf"" unique-identifier=""BookId"" version=""2.0"">
  <metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:opf=""http://www.idpf.org/2007/opf"">
    <dc:title>Old</dc:title>
    <dc:identifier id=""BookId"">urn:uuid:1</dc:identifier>
  </metadata>
</package>");

            var update = new EbookFileMetadata { Title = "Fresh" };
            EpubFileMetadataWriter.ApplyMetadata(opf, update, false, null, "OEBPS/content.opf");

            opf.Root.Element("{http://www.idpf.org/2007/opf}metadata")
                .Element("{http://purl.org/dc/elements/1.1/}title")
                .Value.Should().Be("Fresh");
        }

        internal static void CreateMinimalEpub(string path, string title, string author)
        {
            using (var stream = File.Create(path))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                var mime = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
                using (var mimeStream = mime.Open())
                {
                    var bytes = Encoding.ASCII.GetBytes("application/epub+zip");
                    mimeStream.Write(bytes, 0, bytes.Length);
                }

                var containerXml = @"<?xml version=""1.0""?>
<container version=""1.0"" xmlns=""urn:oasis:names:tc:opendocument:xmlns:container"">
  <rootfiles>
    <rootfile full-path=""OEBPS/content.opf"" media-type=""application/oebps-package+xml""/>
  </rootfiles>
</container>";
                WriteEntry(zip, "META-INF/container.xml", containerXml);

                var opfXml = $@"<?xml version=""1.0""?>
<package xmlns=""http://www.idpf.org/2007/opf"" unique-identifier=""BookId"" version=""2.0"">
  <metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:opf=""http://www.idpf.org/2007/opf"">
    <dc:title>{title}</dc:title>
    <dc:creator opf:role=""aut"">{author}</dc:creator>
    <dc:identifier id=""BookId"">urn:uuid:11111111-1111-1111-1111-111111111111</dc:identifier>
    <dc:language>en</dc:language>
  </metadata>
  <manifest>
    <item id=""ch1"" href=""chapter.xhtml"" media-type=""application/xhtml+xml""/>
  </manifest>
  <spine>
    <itemref idref=""ch1""/>
  </spine>
</package>";
                WriteEntry(zip, "OEBPS/content.opf", opfXml);

                var chapterXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<html xmlns=""http://www.w3.org/1999/xhtml""><head><title>c</title></head><body><p>Hi</p></body></html>";
                WriteEntry(zip, "OEBPS/chapter.xhtml", chapterXml);
            }
        }

        private static void WriteEntry(ZipArchive zip, string name, string content)
        {
            var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.Write(content);
            }
        }
    }
}
