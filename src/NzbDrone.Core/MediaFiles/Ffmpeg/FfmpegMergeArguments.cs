using System;

namespace NzbDrone.Core.MediaFiles.Ffmpeg
{
    public static class FfmpegMergeArguments
    {
        public static string Quote(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return "\"" + path.Replace("\"", "\\\"") + "\"";
        }

        public static string Build(string concatListPath, string chaptersPath, string outputPath, AudiobookMergeFormat format, bool copyCodec)
        {
            if (string.IsNullOrWhiteSpace(concatListPath))
            {
                throw new ArgumentException("Concat list path is required", nameof(concatListPath));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path is required", nameof(outputPath));
            }

            var args = "-hide_banner -nostdin -y -f concat -safe 0 -i " + Quote(concatListPath);

            if (!string.IsNullOrWhiteSpace(chaptersPath))
            {
                args += " -i " + Quote(chaptersPath) + " -map 0:a -map_metadata 1";
            }
            else
            {
                args += " -map 0:a";
            }

            if (copyCodec)
            {
                args += " -c copy";
            }
            else if (format == AudiobookMergeFormat.Mp3)
            {
                args += " -c:a libmp3lame -q:a 2";
            }
            else
            {
                args += " -c:a aac -b:a 64k -movflags +faststart";
            }

            args += " " + Quote(outputPath);
            return args;
        }

        public static string VersionArgs()
        {
            return "-hide_banner -version";
        }

        public static string ProbeDurationArgs(string inputPath)
        {
            return "-v error -show_entries format=duration -of csv=p=0 " + Quote(inputPath);
        }

        public static string OutputExtension(AudiobookMergeFormat format)
        {
            return format == AudiobookMergeFormat.Mp3 ? ".mp3" : ".m4b";
        }
    }
}
