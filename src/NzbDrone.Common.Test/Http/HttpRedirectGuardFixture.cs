using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Http;

namespace NzbDrone.Common.Test.Http
{
    [TestFixture]
    public class HttpRedirectGuardFixture
    {
        [Test]
        public void allows_https_to_https_public_host()
        {
            HttpRedirectGuard.CanFollow(
                new HttpUri("https://indexer.example/feed"),
                new HttpUri("https://cdn.example/file.nzb")).Should().BeTrue();
        }

        [Test]
        public void blocks_non_http_scheme()
        {
            HttpRedirectGuard.CanFollow(
                new HttpUri("https://indexer.example/feed"),
                new HttpUri("ftp://files.example/book.epub")).Should().BeFalse();
        }

        [Test]
        public void blocks_loopback_ip_from_public_origin()
        {
            HttpRedirectGuard.CanFollow(
                new HttpUri("https://indexer.example/feed"),
                new HttpUri("http://127.0.0.1/secret")).Should().BeFalse();
        }

        [Test]
        public void blocks_link_local_metadata_ip()
        {
            HttpRedirectGuard.CanFollow(
                new HttpUri("https://indexer.example/feed"),
                new HttpUri("http://169.254.169.254/latest/meta-data/")).Should().BeFalse();
        }

        [Test]
        public void blocks_rfc1918_from_public_origin()
        {
            HttpRedirectGuard.CanFollow(
                new HttpUri("https://indexer.example/feed"),
                new HttpUri("http://192.168.1.5:8080/")).Should().BeFalse();
        }

        [Test]
        public void allows_same_host_private_redirect()
        {
            HttpRedirectGuard.CanFollow(
                new HttpUri("http://192.168.1.5:8080/login"),
                new HttpUri("http://192.168.1.5:8080/library")).Should().BeTrue();
        }

        [Test]
        public void allows_same_hostname_redirect()
        {
            HttpRedirectGuard.CanFollow(
                new HttpUri("http://calibre:8080/"),
                new HttpUri("http://calibre:8080/browse")).Should().BeTrue();
        }
    }
}
