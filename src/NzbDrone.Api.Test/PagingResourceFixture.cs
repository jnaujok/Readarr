using FluentAssertions;
using NUnit.Framework;
using Readarr.Http;

namespace NzbDrone.Api.Test
{
    [TestFixture]
    public class PagingResourceFixture
    {
        [Test]
        public void should_clamp_page_size_to_maximum()
        {
            var resource = new PagingResource<object>(new PagingRequestResource
            {
                Page = 1,
                PageSize = 100000
            });

            resource.PageSize.Should().Be(PagingRequestResource.MaxPageSize);
        }

        [Test]
        public void should_clamp_page_and_page_size_to_minimum()
        {
            var resource = new PagingResource<object>(new PagingRequestResource
            {
                Page = 0,
                PageSize = 0
            });

            resource.Page.Should().Be(1);
            resource.PageSize.Should().Be(1);
        }
    }
}
