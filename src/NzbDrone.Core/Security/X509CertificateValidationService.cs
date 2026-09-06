using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http.Dispatchers;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Security
{
    public class X509CertificateValidationService : ICertificateValidationService
    {
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public X509CertificateValidationService(IConfigService configService, Logger logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public bool ShouldByPassValidationError(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var targetHostName = string.Empty;

            if (sender is SslStream request)
            {
                targetHostName = request.TargetHostName;
            }
            else if (sender is string stringHost)
            {
                targetHostName = stringHost;
            }
            else if (sslPolicyErrors != SslPolicyErrors.None)
            {
                _logger.Error("Certificate validation failed for unknown sender type {0}. {1}", sender?.GetType().FullName ?? "null", sslPolicyErrors);
                return false;
            }

            if (certificate is X509Certificate2 cert2 && cert2.SignatureAlgorithm.FriendlyName == "md5RSA")
            {
                _logger.Error("https://{0} uses the obsolete md5 hash in it's https certificate, if that is your certificate, please (re)create certificate with better algorithm as soon as possible.", targetHostName);
            }

            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (IsLoopbackHost(targetHostName))
            {
                return true;
            }

            var certificateValidation = _configService.CertificateValidation;

            if (certificateValidation == CertificateValidationType.Disabled)
            {
                return true;
            }

            if (certificateValidation == CertificateValidationType.DisabledForLocalAddresses &&
                IsLocalIpLiteral(targetHostName))
            {
                return true;
            }

            _logger.Error("Certificate validation for {0} failed. {1}", targetHostName, sslPolicyErrors);

            return false;
        }

        public static bool IsLoopbackHost(string targetHostName)
        {
            if (string.Equals(targetHostName, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return IPAddress.TryParse(targetHostName, out var ipAddress) && IPAddress.IsLoopback(ipAddress);
        }

        public static bool IsLocalIpLiteral(string targetHostName)
        {
            if (!IPAddress.TryParse(targetHostName, out var ipAddress))
            {
                return false;
            }

            return ipAddress.IsIPv6LinkLocal || ipAddress.IsLocalAddress();
        }
    }
}
