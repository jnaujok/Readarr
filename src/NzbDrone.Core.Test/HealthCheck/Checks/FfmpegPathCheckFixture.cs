using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.MediaFiles.Ffmpeg;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class FfmpegPathCheckFixture : CoreTest<FfmpegPathCheck>
    {
        [Test]
        public void ok_when_merge_disabled()
        {
            Mocker.GetMock<IConfigService>().SetupGet(c => c.MergeAudiobookParts).Returns(false);
            Subject.Check().Type.Should().Be(HealthCheckResult.Ok);
        }

        [Test]
        public void warning_when_enabled_and_ffmpeg_missing()
        {
            Mocker.GetMock<IConfigService>().SetupGet(c => c.MergeAudiobookParts).Returns(true);
            Mocker.GetMock<IFfmpegExecutable>().Setup(s => s.IsAvailable()).Returns(false);
            Subject.Check().Type.Should().Be(HealthCheckResult.Warning);
        }

        [Test]
        public void ok_when_enabled_and_ffmpeg_found()
        {
            Mocker.GetMock<IConfigService>().SetupGet(c => c.MergeAudiobookParts).Returns(true);
            Mocker.GetMock<IFfmpegExecutable>().Setup(s => s.IsAvailable()).Returns(true);
            Subject.Check().Type.Should().Be(HealthCheckResult.Ok);
        }
    }
}
