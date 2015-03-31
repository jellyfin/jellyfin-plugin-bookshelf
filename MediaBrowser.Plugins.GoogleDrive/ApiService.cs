using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Net;
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
                return string.Format("Emby/{0} +http://emby.media/", version);
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

            try
            {
                var resultStream = await _httpClient.Get(httpRequest);
                return _jsonSerializer.DeserializeFromStream<T>(resultStream);
            }
            catch (HttpException ex)
            {
                var webException = ex.InnerException as WebException;
                if (webException != null)
                {
                    ThrowExceptionWithMessage(webException);
                }
                throw;
            }
        }

        protected async Task<T> PostRequest<T>(string url, IDictionary<string, string> data, CancellationToken cancellationToken)
        {
            var httpRequest = PrepareHttpRequestOptions(url, cancellationToken);
            httpRequest.SetPostData(data);

            try
            {
                var result = await _httpClient.Post(httpRequest);
                return _jsonSerializer.DeserializeFromStream<T>(result.Content);
            }
            catch (HttpException ex)
            {
                var webException = ex.InnerException as WebException;
                if (webException != null)
                {
                    ThrowExceptionWithMessage(webException);
                }
                throw;
            }
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

        private void ThrowExceptionWithMessage(WebException webException)
        {
            var errorDescription = GetErrorDescription(webException);

            throw new ApiException(errorDescription);
        }

        private string GetErrorDescription(WebException webException)
        {
            var stream = webException.Response.GetResponseStream();
            var response = _jsonSerializer.DeserializeFromStream<GoogleError>(stream);
            return GetErrorMessage(response);
        }

        private static string GetErrorMessage(GoogleError response)
        {
            if (!string.IsNullOrEmpty(response.error_description))
            {
                return response.error_description;
            }

            if (response.error == "invalid_grant")
            {
                return "Invalid code.";
            }

            if (response.error == "invalid_client")
            {
                return "Invalid client id or secret.";
            }

            return null;
        }
    }
}
