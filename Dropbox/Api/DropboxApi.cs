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

        public async Task<MetadataResult> Metadata(string path, string accessToken, CancellationToken cancellationToken)
        {
            var url = string.Format("/metadata/auto{0}?file_limit=25000&include_deleted=false", path);

            return await GetRequest<MetadataResult>(url, accessToken, cancellationToken);
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

        public async Task<MediaResult> Media(string path, string accessToken, CancellationToken cancellationToken)
        {
            var url = "/media/auto" + path;
            var data = new Dictionary<string,string>();

            return await PostRequest<MediaResult>(url, accessToken, data, cancellationToken);
        }

        public async Task<DeltaResult> Delta(string cursor, string accessToken, CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(cursor))
            {
                data["cursor"] = cursor;
            }

            return await PostRequest<DeltaResult>("/delta", accessToken, data, cancellationToken);
        }
    }
}
