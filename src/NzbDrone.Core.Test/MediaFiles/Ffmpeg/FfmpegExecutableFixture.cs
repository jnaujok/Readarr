using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Ffmpeg;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.Ffmpeg
{
    [TestFixture]
    public class FfmpegExecutableFixture : CoreTest<FfmpegExecutable>
    {
        [Test]
        public void uses_configured_path()
        {
            Mocker.GetMock<IConfigService>().SetupGet(c => c.FfmpegPath).Returns("/opt/ffmpeg");
            Subject.GetFfmpegPath().Should().Be("/opt/ffmpeg");
        }

        [Test]
        public void defaults_to_ffmpeg_on_path()
        {
            Mocker.GetMock<IConfigService>().SetupGet(c => c.FfmpegPath).Returns("");
            Subject.GetFfmpegPath().Should().Be("ffmpeg");
        }

        [Test]
        public void is_available_when_version_exits_zero()
        {
            Mocker.GetMock<IConfigService>().SetupGet(c => c.FfmpegPath).Returns("ffmpeg");
            Mocker.GetMock<IProcessProvider>()
                .Setup(s => s.StartAndCapture("ffmpeg", FfmpegMergeArguments.VersionArgs(), null))
                .Returns(new ProcessOutput { ExitCode = 0 });

            Subject.IsAvailable().Should().BeTrue();
        }

        [Test]
        public void is_not_available_when_process_throws()
        {
            Mocker.GetMock<IConfigService>().SetupGet(c => c.FfmpegPath).Returns("ffmpeg");
            Mocker.GetMock<IProcessProvider>()
                .Setup(s => s.StartAndCapture(It.IsAny<string>(), It.IsAny<string>(), null))
                .Throws(new System.Exception("not found"));

            Subject.IsAvailable().Should().BeFalse();
        }

        [Test]
        public void ffprobe_uses_sibling_when_present()
        {
            Mocker.GetMock<IConfigService>().SetupGet(c => c.FfmpegPath).Returns("/opt/bin/ffmpeg");
            Mocker.GetMock<IDiskProvider>().Setup(d => d.FileExists("/opt/bin/ffprobe")).Returns(true);
            Subject.GetFfprobePath().Should().Be("/opt/bin/ffprobe");
        }
    }
}
