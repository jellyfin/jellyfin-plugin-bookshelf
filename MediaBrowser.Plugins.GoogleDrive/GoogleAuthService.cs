using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public class GoogleAuthService : ApiService, IGoogleAuthService
    {
        public GoogleAuthService(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
            : base(httpClient, jsonSerializer, applicationHost)
        {}

        protected override string GetBaseUrl(CancellationToken cancellationToken)
        {
            return "https://accounts.google.com/o/oauth2/";
        }

        public async Task<AuthorizationAccessToken> GetToken(string code, string redirectUri, string clientId, string clientSecret, CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, string>
            {
                { "code", code },
                { "redirect_uri", WebUtility.UrlEncode(redirectUri) },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "grant_type", "authorization_code" }
            };

            return await PostRequest<AuthorizationAccessToken>("/token", data, cancellationToken);
        }
    }
}
