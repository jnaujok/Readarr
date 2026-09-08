using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NzbDrone.Core.MediaFiles.Ffmpeg
{
    public class AudiobookChapter
    {
        public string Title { get; set; }
        public double DurationSeconds { get; set; }
    }

    public static class FfmpegChapterMetadata
    {
        public static string Build(IEnumerable<AudiobookChapter> chapters)
        {
            if (chapters == null)
            {
                throw new ArgumentNullException(nameof(chapters));
            }

            var builder = new StringBuilder();
            builder.AppendLine(";FFMETADATA1");

            var startMs = 0L;

            foreach (var chapter in chapters)
            {
                var durationMs = (long)Math.Round(Math.Max(chapter.DurationSeconds, 0) * 1000.0);
                var endMs = startMs + Math.Max(durationMs, 1);

                builder.AppendLine("[CHAPTER]");
                builder.AppendLine("TIMEBASE=1/1000");
                builder.Append("START=");
                builder.AppendLine(startMs.ToString(CultureInfo.InvariantCulture));
                builder.Append("END=");
                builder.AppendLine(endMs.ToString(CultureInfo.InvariantCulture));
                builder.Append("title=");
                builder.AppendLine(Escape(chapter.Title));

                startMs = endMs;
            }

            return builder.ToString();
        }

        public static bool TryParseDurationSeconds(string ffprobeOutput, out double seconds)
        {
            seconds = 0;
            if (string.IsNullOrWhiteSpace(ffprobeOutput))
            {
                return false;
            }

            foreach (var line in ffprobeOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (double.TryParse(line.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out seconds) &&
                    seconds >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string Escape(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return "Chapter";
            }

            return title.Replace("\\", "\\\\").Replace("=", "\\=").Replace(";", "\\;").Replace("#", "\\#");
        }
    }
}
