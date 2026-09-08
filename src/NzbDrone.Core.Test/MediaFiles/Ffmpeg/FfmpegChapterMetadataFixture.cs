using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.Ffmpeg;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.Ffmpeg
{
    [TestFixture]
    public class FfmpegChapterMetadataFixture : CoreTest
    {
        [Test]
        public void builds_sequential_chapters()
        {
            var text = FfmpegChapterMetadata.Build(new[]
            {
                new AudiobookChapter { Title = "Part 1", DurationSeconds = 10 },
                new AudiobookChapter { Title = "Part 2", DurationSeconds = 5.5 }
            });

            text.Should().StartWith(";FFMETADATA1");
            text.Should().Contain("START=0");
            text.Should().Contain("END=10000");
            text.Should().Contain("START=10000");
            text.Should().Contain("END=15500");
            text.Should().Contain("title=Part 1");
            text.Should().Contain("title=Part 2");
        }

        [Test]
        public void parses_ffprobe_duration()
        {
            FfmpegChapterMetadata.TryParseDurationSeconds("12.345\n", out var seconds).Should().BeTrue();
            seconds.Should().BeApproximately(12.345, 0.0001);
        }

        [Test]
        public void parse_duration_rejects_garbage()
        {
            FfmpegChapterMetadata.TryParseDurationSeconds("N/A", out _).Should().BeFalse();
        }
    }
}
