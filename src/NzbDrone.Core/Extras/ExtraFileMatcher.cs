using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NzbDrone.Core.Extras
{
    public static class ExtraFileMatcher
    {
        public static bool IsWanted(string path, IEnumerable<string> wantedExtensions)
        {
            if (string.IsNullOrWhiteSpace(path) || wantedExtensions == null)
            {
                return false;
            }

            var extension = Path.GetExtension(path).TrimStart('.');

            return wantedExtensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }
    }
}
