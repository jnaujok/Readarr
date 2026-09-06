using System;
using System.Net;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Common.Http
{
    public static class HttpRedirectGuard
    {
        public static bool CanFollow(HttpUri from, HttpUri to)
        {
            if (to == null || to.Scheme.IsNullOrWhiteSpace())
            {
                return false;
            }

            if (!to.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                !to.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!TryParseHostIp(to.Host, out var destination))
            {
                return true;
            }

            if (IsLoopbackLinkLocalOrCgnat(destination))
            {
                return HostsMatch(from, to);
            }

            if (IsPrivateNetwork(destination))
            {
                return HostsMatch(from, to);
            }

            return true;
        }

        private static bool TryParseHostIp(string host, out IPAddress address)
        {
            address = null;

            if (host.IsNullOrWhiteSpace())
            {
                return false;
            }

            var trimmed = host.Trim();
            if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(1, trimmed.Length - 2);
            }

            return IPAddress.TryParse(trimmed, out address);
        }

        private static bool HostsMatch(HttpUri from, HttpUri to)
        {
            if (from == null || from.Host.IsNullOrWhiteSpace() || to.Host.IsNullOrWhiteSpace())
            {
                return false;
            }

            return from.Host.Equals(to.Host, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLoopbackLinkLocalOrCgnat(IPAddress ipAddress)
        {
            if (ipAddress.IsIPv4MappedToIPv6)
            {
                ipAddress = ipAddress.MapToIPv4();
            }

            if (IPAddress.IsLoopback(ipAddress) || ipAddress.IsIPv6LinkLocal || ipAddress.IsCgnatIpAddress())
            {
                return true;
            }

            var bytes = ipAddress.GetAddressBytes();
            return bytes.Length == 4 && bytes[0] == 169 && bytes[1] == 254;
        }

        private static bool IsPrivateNetwork(IPAddress ipAddress)
        {
            if (ipAddress.IsIPv4MappedToIPv6)
            {
                ipAddress = ipAddress.MapToIPv4();
            }

            return ipAddress.IsLocalAddress() &&
                   !IPAddress.IsLoopback(ipAddress) &&
                   !IsLoopbackLinkLocalOrCgnat(ipAddress);
        }
    }
}
