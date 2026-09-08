using System;
using System.Collections.Generic;
using System.Text;

namespace NzbDrone.Core.MediaFiles.Ffmpeg
{
    public static class FfmpegConcatList
    {
        public static string Build(IEnumerable<string> paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            var builder = new StringBuilder();

            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                builder.Append("file '");
                builder.Append(path.Replace("'", "'\\''"));
                builder.AppendLine("'");
            }

            return builder.ToString();
        }
    }
}
