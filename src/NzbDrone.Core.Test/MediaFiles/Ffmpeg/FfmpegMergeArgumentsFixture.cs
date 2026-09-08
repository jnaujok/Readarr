using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.Ffmpeg;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.Ffmpeg
{
    [TestFixture]
    public class FfmpegMergeArgumentsFixture : CoreTest
    {
        [Test]
        public void m4b_reencode_includes_aac_and_faststart()
        {
            var args = FfmpegMergeArguments.Build("/tmp/c.txt", "/tmp/ch.txt", "/lib/out.m4b", AudiobookMergeFormat.M4b, false);
            args.Should().Contain("-f concat");
            args.Should().Contain("-c:a aac");
            args.Should().Contain("-movflags +faststart");
            args.Should().Contain("\"/lib/out.m4b\"");
        }

        [Test]
        public void mp3_copy_uses_c_copy()
        {
            var args = FfmpegMergeArguments.Build("/tmp/c.txt", null, "/lib/out.mp3", AudiobookMergeFormat.Mp3, true);
            args.Should().Contain("-c copy");
            args.Should().NotContain("libmp3lame");
            args.Should().NotContain("map_metadata");
        }

        [Test]
        public void output_extension_matches_format()
        {
            FfmpegMergeArguments.OutputExtension(AudiobookMergeFormat.M4b).Should().Be(".m4b");
            FfmpegMergeArguments.OutputExtension(AudiobookMergeFormat.Mp3).Should().Be(".mp3");
        }
    }
}
