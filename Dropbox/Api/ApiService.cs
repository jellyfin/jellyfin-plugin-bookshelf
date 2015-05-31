using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace Dropbox.Api
{
    public abstract class ApiService
    {
        // 1 hour
        private const int TimeoutInMilliseconds = 60 * 60 * 1000;

        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IApplicationHost _applicationHost;

        private string UserAgent
        {
            get
            {
                var version = _applicationHost.ApplicationVersion.ToString();
                return string.Format("Emby/{0} +http://emby.media/", version);
            }
        }

        protected abstract string BaseUrl { get; }

        protected ApiService(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _applicationHost = applicationHost;
        }

        protected async Task<T> GetRequest<T>(string url, string accessToken, CancellationToken cancellationToken)
        {
            var httpRequest = PrepareHttpRequestOptions(url, accessToken, cancellationToken);
            var resultStream = await _httpClient.Get(httpRequest);
            return _jsonSerializer.DeserializeFromStream<T>(resultStream);
        }

        protected async Task<Stream> GetRawRequest(string url, string accessToken, CancellationToken cancellationToken)
        {
            var httpRequest = PrepareHttpRequestOptions(url, accessToken, cancellationToken);
            return await _httpClient.Get(httpRequest);
        }

        protected async Task<T> PostRequest<T>(string url, string accessToken, IDictionary<string, string> data, CancellationToken cancellationToken)
        {
            var httpRequest = PrepareHttpRequestOptions(url, accessToken, cancellationToken);
            httpRequest.SetPostData(data);
            var result = await _httpClient.Post(httpRequest);
            return _jsonSerializer.DeserializeFromStream<T>(result.Content);
        }

        protected async Task<T> PutRequest<T>(string url, string accessToken, byte[] content, CancellationToken cancellationToken)
        {
            var httpRequest = PrepareHttpRequestOptions(url, accessToken, cancellationToken);
            httpRequest.TimeoutMs = TimeoutInMilliseconds;
            httpRequest.RequestContentType = "text/plain";
            httpRequest.RequestContentBytes = content;
            var result = await _httpClient.SendAsync(httpRequest, "PUT");
            return _jsonSerializer.DeserializeFromStream<T>(result.Content);
        }

        private HttpRequestOptions PrepareHttpRequestOptions(string url, string accessToken, CancellationToken cancellationToken)
        {
            var httpRequestOptions = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = BaseUrl + url.TrimStart('/'),
                UserAgent = UserAgent
            };

            if (!string.IsNullOrEmpty(accessToken))
            {
                httpRequestOptions.RequestHeaders["Authorization"] = "Bearer " + accessToken;
            }

            return httpRequestOptions;
        }
    }
}
