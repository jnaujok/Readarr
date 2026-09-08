using System;
using System.IO;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.MediaFiles.Ffmpeg
{
    public interface IFfmpegExecutable
    {
        string GetFfmpegPath();
        string GetFfprobePath();
        bool IsAvailable();
    }

    public class FfmpegExecutable : IFfmpegExecutable
    {
        private readonly IConfigService _configService;
        private readonly IProcessProvider _processProvider;
        private readonly IDiskProvider _diskProvider;

        public FfmpegExecutable(IConfigService configService, IProcessProvider processProvider, IDiskProvider diskProvider)
        {
            _configService = configService;
            _processProvider = processProvider;
            _diskProvider = diskProvider;
        }

        public string GetFfmpegPath()
        {
            var configured = _configService.FfmpegPath;
            if (configured.IsNotNullOrWhiteSpace())
            {
                return configured.Trim();
            }

            return "ffmpeg";
        }

        public string GetFfprobePath()
        {
            var ffmpeg = GetFfmpegPath();
            var directory = Path.GetDirectoryName(ffmpeg);
            if (directory.IsNotNullOrWhiteSpace())
            {
                var sibling = Path.Combine(directory, AppendExe("ffprobe"));
                if (_diskProvider.FileExists(sibling))
                {
                    return sibling;
                }
            }

            return "ffprobe";
        }

        public bool IsAvailable()
        {
            try
            {
                var output = _processProvider.StartAndCapture(GetFfmpegPath(), FfmpegMergeArguments.VersionArgs());
                return output.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private static string AppendExe(string name)
        {
            if (Path.DirectorySeparatorChar == '\\' && !name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return name + ".exe";
            }

            return name;
        }
    }
}
