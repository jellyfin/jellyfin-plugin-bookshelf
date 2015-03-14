using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public abstract class ApiService
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IApplicationHost _applicationHost;

        private string UserAgent
        {
            get
            {
                var version = _applicationHost.ApplicationVersion.ToString();
                return string.Format("Media Browser/{0} +http://mediabrowser.tv/", version);
            }
        }

        protected abstract string GetBaseUrl(CancellationToken cancellationToken);

        protected ApiService(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _applicationHost = applicationHost;
        }

        protected virtual void BeforeExecute(HttpRequestOptions options)
        {
            // Do nothing
        }

        protected async Task<T> GetRequest<T>(string url, CancellationToken cancellationToken)
        {
            var httpRequest = PrepareHttpRequestOptions(url, cancellationToken);
            var resultStream = await _httpClient.Get(httpRequest);
            return _jsonSerializer.DeserializeFromStream<T>(resultStream);
        }

        protected async Task<T> PostRequest<T>(string url, IDictionary<string, string> data, CancellationToken cancellationToken)
        {
            var httpRequest = PrepareHttpRequestOptions(url, cancellationToken);
            httpRequest.SetPostData(data);
            var result = await _httpClient.Post(httpRequest);
            return _jsonSerializer.DeserializeFromStream<T>(result.Content);
        }

        private HttpRequestOptions PrepareHttpRequestOptions(string url, CancellationToken cancellationToken)
        {
            var httpRequest = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = BuildUrl(url, cancellationToken),
                UserAgent = UserAgent
            };
            BeforeExecute(httpRequest);
            return httpRequest;
        }

        private string BuildUrl(string url, CancellationToken cancellationToken)
        {
            return GetBaseUrl(cancellationToken) + url.TrimStart('/');
        }
    }
}
