using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.Ffmpeg;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.Ffmpeg
{
    [TestFixture]
    public class FfmpegConcatListFixture : CoreTest
    {
        [Test]
        public void builds_concat_lines_and_escapes_quotes()
        {
            var text = FfmpegConcatList.Build(new[] { "/lib/a.mp3", "/lib/O'Brien.mp3" });
            text.Should().Contain("file '/lib/a.mp3'");
            text.Should().Contain("file '/lib/O'\\''Brien.mp3'");
        }

        [Test]
        public void skips_blank_paths()
        {
            var text = FfmpegConcatList.Build(new[] { "/lib/a.mp3", "", null, "  " });
            text.Should().Contain("file '/lib/a.mp3'");
            text.Should().NotContain("file ''");
        }
    }
}
