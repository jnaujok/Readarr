using NzbDrone.Core.Configuration;
using NzbDrone.Core.Localization;
using NzbDrone.Core.MediaFiles.Ffmpeg;

namespace NzbDrone.Core.HealthCheck.Checks
{
    public class FfmpegPathCheck : HealthCheckBase
    {
        private readonly IConfigService _configService;
        private readonly IFfmpegExecutable _ffmpeg;

        public FfmpegPathCheck(IConfigService configService, IFfmpegExecutable ffmpeg, ILocalizationService localizationService)
            : base(localizationService)
        {
            _configService = configService;
            _ffmpeg = ffmpeg;
        }

        public override HealthCheck Check()
        {
            if (!_configService.MergeAudiobookParts)
            {
                return new HealthCheck(GetType());
            }

            if (_ffmpeg.IsAvailable())
            {
                return new HealthCheck(GetType());
            }

            return new HealthCheck(GetType(),
                HealthCheckResult.Warning,
                "Audiobook part merging is enabled but FFmpeg was not found. Install FFmpeg or set its path under Settings → Media Management.",
                "ffmpeg-not-found");
        }
    }
}
