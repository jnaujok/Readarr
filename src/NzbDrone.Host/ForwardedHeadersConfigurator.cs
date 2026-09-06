using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Common.Network;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Host
{
    public static class ForwardedHeadersConfigurator
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(ForwardedHeadersConfigurator));

        public static void Configure(ForwardedHeadersOptions options, IConfigFileProvider configFileProvider)
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;

            // Keep ASP.NET defaults (loopback). Do not Clear() KnownNetworks/KnownProxies —
            // that trusts X-Forwarded-* from any client and allows auth bypass.
            var trustedNetworks = configFileProvider.TrustedNetworks;

            if (trustedNetworks.IsNullOrWhiteSpace())
            {
                return;
            }

            options.ForwardLimit = null;

            foreach (var entry in trustedNetworks.Split(','))
            {
                if (entry.IsNullOrWhiteSpace())
                {
                    continue;
                }

                if (IPNetworkParser.TryParse(entry, out var address, out var prefixLength))
                {
                    options.KnownNetworks.Add(new IPNetwork(address, prefixLength));

                    Logger.Info("Trusting forwarded headers from {0}/{1}", address, prefixLength);
                }
                else
                {
                    Logger.Warn("Invalid trusted network '{0}', forwarded headers from it will not be trusted", entry.Trim());
                }
            }
        }
    }
}
