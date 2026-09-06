using System;
using System.Collections.Generic;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Common
{
    public class PathEqualityComparer : IEqualityComparer<string>
    {
        public static readonly PathEqualityComparer Instance = new PathEqualityComparer();

        private PathEqualityComparer()
        {
        }

        public bool Equals(string x, string y)
        {
            return x.PathEquals(y);
        }

        public int GetHashCode(string obj)
        {
            return GetHashCode(obj, OsInfo.IsWindows);
        }

        public static int GetHashCode(string path, bool ignoreCase)
        {
            var cleaned = path.CleanFilePath();

            return ignoreCase
                ? StringComparer.OrdinalIgnoreCase.GetHashCode(cleaned)
                : cleaned.GetHashCode();
        }
    }
}
