using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Test.Qualities
{
    [TestFixture]
    public class QualityAudioFixture
    {
        [Test]
        public void ebook_qualities_are_not_audio()
        {
            Quality.Unknown.IsAudio.Should().BeFalse();
            Quality.PDF.IsAudio.Should().BeFalse();
            Quality.MOBI.IsAudio.Should().BeFalse();
            Quality.EPUB.IsAudio.Should().BeFalse();
            Quality.AZW3.IsAudio.Should().BeFalse();
        }

        [Test]
        public void audio_qualities_are_audio()
        {
            Quality.MP3.IsAudio.Should().BeTrue();
            Quality.FLAC.IsAudio.Should().BeTrue();
            Quality.M4B.IsAudio.Should().BeTrue();
            Quality.UnknownAudio.IsAudio.Should().BeTrue();
        }
    }
}
