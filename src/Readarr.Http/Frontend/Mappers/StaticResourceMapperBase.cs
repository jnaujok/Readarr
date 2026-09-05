using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;

namespace Readarr.Http.Frontend.Mappers
{
    public abstract class StaticResourceMapperBase : IMapHttpRequestsToDisk
    {
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;
        private readonly StringComparison _caseSensitive;
        private readonly IContentTypeProvider _mimeTypeProvider;

        protected StaticResourceMapperBase(IDiskProvider diskProvider, Logger logger)
        {
            _diskProvider = diskProvider;
            _logger = logger;

            _mimeTypeProvider = new FileExtensionContentTypeProvider();
            _caseSensitive = RuntimeInfo.IsProduction ? DiskProviderBase.PathStringComparison : StringComparison.OrdinalIgnoreCase;
        }

        public abstract string Map(string resourceUrl);

        public abstract bool CanHandle(string resourceUrl);

        public IActionResult GetResponse(string resourceUrl)
        {
            var filePath = Map(resourceUrl);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            if (_diskProvider.FileExists(filePath, _caseSensitive))
            {
                if (!_mimeTypeProvider.TryGetContentType(filePath, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                return new FileStreamResult(GetContentStream(filePath), new MediaTypeHeaderValue(contentType)
                {
                    Encoding = contentType == "text/plain" ? Encoding.UTF8 : null
                });
            }

            _logger.Warn("File {0} not found", filePath);

            return null;
        }

        protected virtual Stream GetContentStream(string filePath)
        {
            return File.OpenRead(filePath);
        }

        protected static string ConfineToRoot(string root, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            var relative = relativePath.Replace('/', Path.DirectorySeparatorChar)
                                       .Replace('\\', Path.DirectorySeparatorChar)
                                       .Trim(Path.DirectorySeparatorChar);

            if (Path.IsPathRooted(relative))
            {
                return null;
            }

            var rootFull = Path.GetFullPath(root);
            if (rootFull.Length > 0 &&
                rootFull[rootFull.Length - 1] != Path.DirectorySeparatorChar &&
                rootFull[rootFull.Length - 1] != Path.AltDirectorySeparatorChar)
            {
                rootFull += Path.DirectorySeparatorChar;
            }

            var fullPath = Path.GetFullPath(Path.Combine(rootFull, relative));

            if (!fullPath.StartsWith(rootFull, DiskProviderBase.PathStringComparison))
            {
                return null;
            }

            return fullPath;
        }
    }
}
