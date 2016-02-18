namespace LastfmScrobbler.Api
{
    using MediaBrowser.Common.Net;
    using MediaBrowser.Model.Serialization;
    using Models.Requests;
    using Models.Responses;
    using Resources;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Utils;

    public class BaseLastfmApiClient
    {
        private const string ApiVersion = "2.0";

        private readonly IHttpClient     _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public BaseLastfmApiClient(IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _httpClient     = httpClient;
            _jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// Send a POST request to the LastFM Api
        /// </summary>
        /// <typeparam name="TRequest">The type of the request</typeparam>
        /// <typeparam name="TResponse">The type of the response</typeparam>
        /// <param name="request">The request</param>
        /// <returns>A response with type TResponse</returns>
        public async Task<TResponse> Post<TRequest, TResponse>(TRequest request) where TRequest: BaseRequest where TResponse: BaseResponse
        {
            var data = request.ToDictionary();

            //Append the signature
            Helpers.AppendSignature(ref data);

            using (var stream = await _httpClient.Post(new HttpRequestOptions
            {
                Url                   = BuildPostUrl(request.Secure),
                ResourcePool          = Plugin.LastfmResourcePool,
                CancellationToken     = CancellationToken.None,
                EnableHttpCompression = false,
            }, EscapeDictionary(data)))
            {
                try
                {
                    var result = _jsonSerializer.DeserializeFromStream<TResponse>(stream);

                    //Lets Log the error here to ensure all errors are logged
                    if (result.IsError())
                        Plugin.Logger.Error(result.Message);

                    return result;
                }
                catch (Exception e)
                {
                    Plugin.Logger.Debug(e.Message);
                }

                return null;
            }
        }

        public async Task<TResponse> Get<TRequest, TResponse>(TRequest request) where TRequest: BaseRequest where TResponse: BaseResponse
        {
            return await Get<TRequest, TResponse>(request, CancellationToken.None);
        }

        public async Task<TResponse> Get<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken) where TRequest: BaseRequest where TResponse: BaseResponse
        {
            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url                   = BuildGetUrl(request.ToDictionary()),
                ResourcePool          = Plugin.LastfmResourcePool,
                CancellationToken     = cancellationToken,
                EnableHttpCompression = false,
            }))
            {
                try
                {
                    var result = _jsonSerializer.DeserializeFromStream<TResponse>(stream);

                    //Lets Log the error here to ensure all errors are logged
                    if (result.IsError())
                        Plugin.Logger.Error(result.Message);

                    return result;
                }
                catch (Exception e)
                {
                    Plugin.Logger.Debug(e.Message);
                }

                return null;
            }
        }

        #region Private methods
        private static string BuildGetUrl(Dictionary<string, string> requestData)
        {
            return String.Format("http://{0}/{1}/?format=json&{2}",
                                    Strings.Endpoints.LastfmApi,
                                    ApiVersion,
                                    Helpers.DictionaryToQueryString(requestData)
                                );
        }

        private static string BuildPostUrl(bool secure = false)
        {
            return String.Format("{0}://{1}/{2}/?format=json",
                                    secure ? "https" : "http",
                                    Strings.Endpoints.LastfmApi,
                                    ApiVersion
                                );
        }

        private Dictionary<string, string> EscapeDictionary(Dictionary<string, string> dic)
        {
            return dic.ToDictionary(item => item.Key, item => Uri.EscapeDataString(item.Value));
        }
        #endregion
    }
}
