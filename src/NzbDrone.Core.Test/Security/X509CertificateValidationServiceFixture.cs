using System.Net.Security;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Security;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Security
{
    [TestFixture]
    public class X509CertificateValidationServiceFixture : CoreTest<X509CertificateValidationService>
    {
        [Test]
        public void valid_certificate_is_accepted_for_unknown_sender()
        {
            Subject.ShouldByPassValidationError(123, null, null, SslPolicyErrors.None).Should().BeTrue();
        }

        [Test]
        public void unknown_sender_with_errors_fails_closed()
        {
            Subject.ShouldByPassValidationError(123, null, null, SslPolicyErrors.RemoteCertificateNameMismatch)
                   .Should()
                   .BeFalse();

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void localhost_label_is_allowed_without_dns()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.CertificateValidation)
                  .Returns(CertificateValidationType.Enabled);

            Subject.ShouldByPassValidationError("localhost", null, null, SslPolicyErrors.RemoteCertificateNameMismatch)
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void loopback_ip_is_allowed_without_dns()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.CertificateValidation)
                  .Returns(CertificateValidationType.Enabled);

            Subject.ShouldByPassValidationError("127.0.0.1", null, null, SslPolicyErrors.RemoteCertificateNameMismatch)
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void hostname_is_not_trusted_via_dns_when_local_validation_is_disabled()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.CertificateValidation)
                  .Returns(CertificateValidationType.DisabledForLocalAddresses);

            Subject.ShouldByPassValidationError("calibre.local", null, null, SslPolicyErrors.RemoteCertificateNameMismatch)
                   .Should()
                   .BeFalse();

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void private_ip_literal_is_allowed_when_local_validation_is_disabled()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.CertificateValidation)
                  .Returns(CertificateValidationType.DisabledForLocalAddresses);

            Subject.ShouldByPassValidationError("10.0.0.5", null, null, SslPolicyErrors.RemoteCertificateNameMismatch)
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void public_ip_literal_is_not_allowed_when_local_validation_is_disabled()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.CertificateValidation)
                  .Returns(CertificateValidationType.DisabledForLocalAddresses);

            Subject.ShouldByPassValidationError("8.8.8.8", null, null, SslPolicyErrors.RemoteCertificateNameMismatch)
                   .Should()
                   .BeFalse();

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void disabled_validation_bypasses_errors()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.CertificateValidation)
                  .Returns(CertificateValidationType.Disabled);

            Subject.ShouldByPassValidationError("example.com", null, null, SslPolicyErrors.RemoteCertificateChainErrors)
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void loopback_host_helper_matches_localhost_and_ips()
        {
            X509CertificateValidationService.IsLoopbackHost("localhost").Should().BeTrue();
            X509CertificateValidationService.IsLoopbackHost("127.0.0.1").Should().BeTrue();
            X509CertificateValidationService.IsLoopbackHost("::1").Should().BeTrue();
            X509CertificateValidationService.IsLoopbackHost("example.com").Should().BeFalse();
        }

        [Test]
        public void local_ip_literal_helper_does_not_resolve_hostnames()
        {
            X509CertificateValidationService.IsLocalIpLiteral("192.168.1.10").Should().BeTrue();
            X509CertificateValidationService.IsLocalIpLiteral("example.com").Should().BeFalse();
        }
    }
}
