using System.IO;
using System.IO.Compression;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ProviderTests.DiskProviderTests
{
    [TestFixture]
    public class ArchiveProviderFixture : TestBase<ArchiveService>
    {
        [Test]
        public void Should_extract_to_correct_folder()
        {
            var destinationFolder = new DirectoryInfo(GetTempFilePath());
            var testArchive = OsInfo.IsWindows ? "TestArchive.zip" : "TestArchive.tar.gz";

            Subject.Extract(GetTestPath("Files/" + testArchive), destinationFolder.FullName);

            destinationFolder.Exists.Should().BeTrue();
            destinationFolder.GetDirectories().Should().HaveCount(1);
            destinationFolder.GetDirectories("*", SearchOption.AllDirectories).Should().HaveCount(3);
            destinationFolder.GetFiles("*.*", SearchOption.AllDirectories).Should().HaveCount(6);
        }

        [Test]
        public void should_reject_zip_entries_that_escape_destination()
        {
            var destination = GetTempFilePath();
            Directory.CreateDirectory(destination);
            var zipPath = GetTempFilePath() + ".zip";

            using (var fileStream = File.Create(zipPath))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("../evil.txt");
                using (var stream = entry.Open())
                {
                    var bytes = Encoding.UTF8.GetBytes("pwned");
                    stream.Write(bytes, 0, bytes.Length);
                }
            }

            Assert.Throws<IOException>(() => Subject.Extract(zipPath, destination));
            File.Exists(Path.Combine(Path.GetDirectoryName(destination), "evil.txt")).Should().BeFalse();
        }
    }
}
