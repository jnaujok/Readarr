using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource
{
    [TestFixture]
    public class MetadataRequestBuilderFixture : CoreTest<MetadataRequestBuilder>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MetadataSource)
                .Returns("");

            Mocker.GetMock<IReadarrCloudRequestBuilder>()
                .Setup(s => s.Metadata)
                .Returns(new HttpRequestBuilder(ReadarrCloudRequestBuilder.DefaultMetadataUrl).CreateFactory());
        }

        private void WithCustomProvider()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MetadataSource)
                .Returns("http://api.readarr.com/api/testing/");
        }

        [TestCase]
        public void should_use_user_definied_if_not_blank()
        {
            WithCustomProvider();

            var details = Subject.GetRequestBuilder().Create();

            details.BaseUrl.ToString().Should().Contain("testing");
        }

        [TestCase]
        public void should_use_default_if_config_blank()
        {
            var details = Subject.GetRequestBuilder().Create();

            details.BaseUrl.ToString().Should().Contain("api.bookinfo.pro");
            details.BaseUrl.ToString().Should().NotContain("/v1/");
        }

        [Test]
        public void default_url_is_rreading_glasses_without_v1_prefix()
        {
            ReadarrCloudRequestBuilder.DefaultMetadataUrl.Should().Be("https://api.bookinfo.pro/{route}");

            var cloud = new ReadarrCloudRequestBuilder();
            cloud.Metadata.Create().BaseUrl.ToString().Should().Contain("api.bookinfo.pro");
            cloud.Metadata.Create().BaseUrl.ToString().Should().NotContain("/v1/");
        }
    }
}
