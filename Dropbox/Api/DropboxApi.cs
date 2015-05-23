using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace Dropbox.Api
{
    public class DropboxApi : ApiService, IDropboxApi
    {
        public DropboxApi(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
            : base(httpClient, jsonSerializer, applicationHost)
        { }

        protected override string BaseUrl
        {
            get { return "https://api.dropbox.com/1/"; }
        }

        public async Task<AuthorizationToken> AcquireToken(string code, string appKey, string appSecret, CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, string>
            {
                { "code", code },
                { "grant_type", "authorization_code" },
                { "client_id", appKey },
                { "client_secret", appSecret }
            };

            return await PostRequest<AuthorizationToken>("/oauth2/token", null, data, cancellationToken);
        }

        public async Task Delete(string path, string accessToken, CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, string>
            {
                { "root", "auto" },
                { "path", path }
            };

            await PostRequest<object>("/fileops/delete", accessToken, data, cancellationToken);
        }

        public async Task<ShareResult> Shares(string path, string accessToken, CancellationToken cancellationToken)
        {
            var url = "/shares/auto/" + path;
            var data = new Dictionary<string, string>
            {
                { "short_url", "false" }
            };

            return await PostRequest<ShareResult>(url, accessToken, data, cancellationToken);
        }
    }
}
