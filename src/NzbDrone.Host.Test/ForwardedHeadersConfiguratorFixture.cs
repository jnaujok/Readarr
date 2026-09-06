using System.Linq;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Test.Common;

namespace NzbDrone.Host.Test
{
    [TestFixture]
    public class ForwardedHeadersConfiguratorFixture : TestBase
    {
        private ForwardedHeadersOptions _options;
        private Mock<IConfigFileProvider> _config;

        [SetUp]
        public void SetUp()
        {
            _options = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.None
            };
            _config = new Mock<IConfigFileProvider>();
        }

        [Test]
        public void should_not_clear_known_loopback_proxies()
        {
            _config.SetupGet(c => c.TrustedNetworks).Returns(string.Empty);

            var knownProxyCount = _options.KnownProxies.Count;
            var knownNetworkCount = _options.KnownNetworks.Count;

            ForwardedHeadersConfigurator.Configure(_options, _config.Object);

            _options.KnownProxies.Count.Should().Be(knownProxyCount);
            _options.KnownNetworks.Count.Should().Be(knownNetworkCount);
            _options.KnownNetworks.Should().NotContain(n => n.Prefix.Equals(IPAddress.Parse("10.0.0.0")));
        }

        [Test]
        public void should_not_trust_rfc1918_when_trusted_networks_unset()
        {
            _config.SetupGet(c => c.TrustedNetworks).Returns((string)null);

            ForwardedHeadersConfigurator.Configure(_options, _config.Object);

            _options.KnownNetworks.Any(n => n.Prefix.Equals(IPAddress.Parse("192.168.0.0"))).Should().BeFalse();
            _options.KnownNetworks.Any(n => n.Prefix.Equals(IPAddress.Parse("10.0.0.0"))).Should().BeFalse();
        }

        [Test]
        public void should_add_configured_trusted_networks()
        {
            _config.SetupGet(c => c.TrustedNetworks).Returns("10.0.0.0/8, 192.168.50.1");

            ForwardedHeadersConfigurator.Configure(_options, _config.Object);

            _options.ForwardLimit.Should().BeNull();
            _options.KnownNetworks.Should().Contain(n => n.Prefix.Equals(IPAddress.Parse("10.0.0.0")) && n.PrefixLength == 8);
            _options.KnownNetworks.Should().Contain(n => n.Prefix.Equals(IPAddress.Parse("192.168.50.1")) && n.PrefixLength == 32);
        }

        [Test]
        public void should_ignore_invalid_trusted_network_entries()
        {
            _config.SetupGet(c => c.TrustedNetworks).Returns("nope, 172.16.0.0/12");

            ForwardedHeadersConfigurator.Configure(_options, _config.Object);

            _options.KnownNetworks.Should().Contain(n => n.Prefix.Equals(IPAddress.Parse("172.16.0.0")) && n.PrefixLength == 12);
            _options.KnownNetworks.Should().NotContain(n => n.Prefix.ToString() == "nope");
        }

        [Test]
        public void should_enable_forwarded_headers()
        {
            _config.SetupGet(c => c.TrustedNetworks).Returns(string.Empty);

            ForwardedHeadersConfigurator.Configure(_options, _config.Object);

            _options.ForwardedHeaders.Should().HaveFlag(ForwardedHeaders.XForwardedFor);
            _options.ForwardedHeaders.Should().HaveFlag(ForwardedHeaders.XForwardedProto);
            _options.ForwardedHeaders.Should().HaveFlag(ForwardedHeaders.XForwardedHost);
        }
    }
}
