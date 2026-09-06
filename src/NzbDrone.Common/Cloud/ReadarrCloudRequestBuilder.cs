using NzbDrone.Common.Http;

namespace NzbDrone.Common.Cloud
{
    public interface IReadarrCloudRequestBuilder
    {
        IHttpRequestBuilderFactory Services { get; }
        IHttpRequestBuilderFactory Metadata { get; }
    }

    public class ReadarrCloudRequestBuilder : IReadarrCloudRequestBuilder
    {
        // rreading-glasses drop-in for the retired BookInfo service.
        // Custom MetadataSource values should be the origin only (no /v1), e.g. https://api.bookinfo.pro
        public const string DefaultMetadataUrl = "https://api.bookinfo.pro/{route}";

        public ReadarrCloudRequestBuilder()
        {
            //TODO: Create Update Endpoint
            Services = new HttpRequestBuilder("https://readarr.servarr.com/v1/")
                .CreateFactory();

            Metadata = new HttpRequestBuilder(DefaultMetadataUrl)
                .CreateFactory();
        }

        public IHttpRequestBuilderFactory Services { get; }

        public IHttpRequestBuilderFactory Metadata { get; }
    }
}
