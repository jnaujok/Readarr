using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration;

namespace Readarr.Http.Frontend.Mappers
{
    public class StaticResourceMapper : StaticResourceMapperBase
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IConfigFileProvider _configFileProvider;

        public StaticResourceMapper(IAppFolderInfo appFolderInfo, IDiskProvider diskProvider, IConfigFileProvider configFileProvider, Logger logger)
            : base(diskProvider, logger)
        {
            _appFolderInfo = appFolderInfo;
            _configFileProvider = configFileProvider;
        }

        public override string Map(string resourceUrl)
        {
            var uiRoot = Path.Combine(_appFolderInfo.StartUpFolder, _configFileProvider.UiFolder);
            var relative = resourceUrl.Replace('/', Path.DirectorySeparatorChar).Trim(Path.DirectorySeparatorChar);

            return ConfineToRoot(uiRoot, relative);
        }

        public override bool CanHandle(string resourceUrl)
        {
            resourceUrl = resourceUrl.ToLowerInvariant();

            if (resourceUrl.StartsWith("/content/images/icons/manifest") ||
                resourceUrl.StartsWith("/content/images/icons/browserconfig"))
            {
                return false;
            }

            return resourceUrl.StartsWith("/content") ||
                   resourceUrl.EndsWith(".js") ||
                   resourceUrl.EndsWith(".map") ||
                   resourceUrl.EndsWith(".css") ||
                   (resourceUrl.EndsWith(".ico") && !resourceUrl.Equals("/favicon.ico")) ||
                   resourceUrl.EndsWith(".swf") ||
                   resourceUrl.EndsWith("oauth.html");
        }
    }
}
