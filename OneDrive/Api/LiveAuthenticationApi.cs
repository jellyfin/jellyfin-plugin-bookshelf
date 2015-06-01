using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace OneDrive.Api
{
    public class LiveAuthenticationApi : ApiService, ILiveAuthenticationApi
    {
        protected override string BaseUrl
        {
            get { return "https://login.live.com/"; }
        }

        public LiveAuthenticationApi(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
            : base(httpClient, jsonSerializer, applicationHost)
        { }

        public Task<AuthorizationToken> AcquireToken(string code, string redirectUrl, string clientId, string clientSecret, CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "code", code },
                { "grant_type", "authorization_code" },
                { "redirect_uri", redirectUrl }
            };

            var httpRequest = PrepareHttpRequestOptions("oauth20_token.srf", null, cancellationToken);
            httpRequest.SetPostData(data);

            return PostRequest<AuthorizationToken>(httpRequest, cancellationToken);
        }

        public Task<AuthorizationToken> RefreshToken(string refreshToken, string redirectUrl, string clientId, string clientSecret, CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" },
                { "redirect_uri", redirectUrl }
            };

            var httpRequest = PrepareHttpRequestOptions("oauth20_token.srf", null, cancellationToken);
            httpRequest.SetPostData(data);

            return PostRequest<AuthorizationToken>(httpRequest, cancellationToken);
        }
    }
}
