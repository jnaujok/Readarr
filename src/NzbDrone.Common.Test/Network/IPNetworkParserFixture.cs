using System.Net;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Network;

namespace NzbDrone.Common.Test.Network
{
    [TestFixture]
    public class IPNetworkParserFixture
    {
        [TestCase("127.0.0.1", "127.0.0.1", 32)]
        [TestCase("10.0.0.0/8", "10.0.0.0", 8)]
        [TestCase("172.16.0.0/12", "172.16.0.0", 12)]
        [TestCase("192.168.0.0/16", "192.168.0.0", 16)]
        [TestCase("192.168.50.1", "192.168.50.1", 32)]
        [TestCase("::1", "::1", 128)]
        [TestCase("fc00::/7", "fc00::", 7)]
        public void should_parse_valid_network(string value, string expectedAddress, int expectedPrefix)
        {
            IPNetworkParser.TryParse(value, out var address, out var prefixLength).Should().BeTrue();
            address.Should().Be(IPAddress.Parse(expectedAddress));
            prefixLength.Should().Be(expectedPrefix);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not-an-ip")]
        [TestCase("1.2.3")]
        [TestCase("192.168.1.0/33")]
        [TestCase("192.168.1.0/0")]
        [TestCase("192.168.1.1/24")]
        [TestCase("10.0.0.0/8/8")]
        public void should_reject_invalid_network(string value)
        {
            IPNetworkParser.TryParse(value, out _, out _).Should().BeFalse();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("10.0.0.0/8")]
        [TestCase("10.0.0.0/8, 192.168.0.0/16")]
        [TestCase("127.0.0.1,::1")]
        public void should_accept_valid_list(string value)
        {
            IPNetworkParser.IsValidList(value).Should().BeTrue();
        }

        [TestCase("nope")]
        [TestCase("10.0.0.0/8, 192.168.1.1/24")]
        public void should_reject_invalid_list(string value)
        {
            IPNetworkParser.IsValidList(value).Should().BeFalse();
        }

        [Test]
        public void should_normalize_list()
        {
            IPNetworkParser.NormalizeList(" 10.0.0.0/8,  192.168.0.0/16 ,")
                           .Should()
                           .Be("10.0.0.0/8, 192.168.0.0/16");
        }
    }
}
