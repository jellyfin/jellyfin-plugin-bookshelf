using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;
using Newtonsoft.Json;

namespace OneDrive.Api
{
    public class OneDriveApi : ApiService, IOneDriveApi
    {
        protected override string BaseUrl
        {
            get { return "https://api.onedrive.com/v1.0/"; }
        }

        public OneDriveApi(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
            : base(httpClient, jsonSerializer, applicationHost)
        { }

        public async Task<UploadSession> CreateUploadSession(string path, OneDriveCredentials credentials, CancellationToken cancellationToken)
        {
            var url = string.Format("/drive/special/approot:/{0}:/upload.createSession", path);
            var accessToken = await credentials.GetAccessToken(cancellationToken);

            return await PostRequest<UploadSession>(url, accessToken, "{ \"@name.conflictBehavior\": \"replace\" }", "application/json", cancellationToken);
        }

        public async Task<UploadSession> UploadFragment(string url, long rangeStart, long rangeEnd, long totalLength, byte[] content, OneDriveCredentials credentials, CancellationToken cancellationToken)
        {
            string accessToken = await credentials.GetAccessToken(cancellationToken);

            var httpRequest = PrepareHttpRequestOptions(string.Empty, accessToken, cancellationToken);
            httpRequest.Url = url;
            // 1 hour
            httpRequest.TimeoutMs = 60 * 60 * 1000;
            httpRequest.RequestContentBytes = content;
            httpRequest.RequestHeaders["Content-Range"] = string.Format("bytes {0}-{1}/{2}", rangeStart, rangeEnd, totalLength);

            return await PutRequest<UploadSession>(httpRequest, cancellationToken);
        }

        public async Task CreateFolder(string path, string name, OneDriveCredentials credentials, CancellationToken cancellationToken)
        {
            string url = string.IsNullOrEmpty(path)
                ? "/drive/special/approot/children"
                : string.Format("/drive/special/approot:/{0}:/children", path);

            string accessToken = await credentials.GetAccessToken(cancellationToken);

            var data = new CreateFolderParameters
            {
                name = name,
                folder = new object(),
                conflictBehavior = "fail"
            };

            var httpRequest = PrepareHttpRequestOptions(url, accessToken, cancellationToken);
            httpRequest.RequestContent = JsonConvert.SerializeObject(data);
            httpRequest.RequestContentType = "application/json";
            httpRequest.LogErrorResponseBody = false;

            await PostRequest<object>(httpRequest, cancellationToken);
        }

        public async Task Delete(string id, OneDriveCredentials credentials, CancellationToken cancellationToken)
        {
            var url = "/drive/items/" + id;
            string accessToken = await credentials.GetAccessToken(cancellationToken);

            await DeleteRequest(url, accessToken, cancellationToken);
        }

        /*public async Task<Stream> Download(string id, OneDriveCredentials credentials, CancellationToken cancellationToken)
        {
            var url = string.Format("/drive/items/{0}/content", id);
            string accessToken = await credentials.GetAccessToken(cancellationToken);

            return await GetRawRequest(url, accessToken, cancellationToken);
        }*/

        public async Task<LinkResult> CreateLink(string id, OneDriveCredentials credentials, CancellationToken cancellationToken)
        {
            var url = string.Format("/drive/items/{0}/action.createLink", id);
            string accessToken = await credentials.GetAccessToken(cancellationToken);

            var data = new CreateLinkParameters
            {
                type = "view"
            };

            return await PostRequest<LinkResult>(url, accessToken, data, cancellationToken);
        }

        public async Task<ViewChangesResult> ViewChangeById(string id, OneDriveCredentials credentials, CancellationToken cancellationToken)
        {
            var url = string.Format("/drive/items/{0}/view.changes", id);
            string accessToken = await credentials.GetAccessToken(cancellationToken);

            return await CallViewChanges(url, accessToken, cancellationToken);
        }

        public async Task<ViewChangesResult> ViewChangeByPath(string path, OneDriveCredentials credentials, CancellationToken cancellationToken)
        {
            var url = string.Format("/drive/special/approot:/{0}:/view.changes", path);
            string accessToken = await credentials.GetAccessToken(cancellationToken);

            return await CallViewChanges(url, accessToken, cancellationToken);
        }

        public async Task<ViewChangesResult> ViewChanges(string token, OneDriveCredentials credentials, CancellationToken cancellationToken)
        {
            var url = "/drive/special/approot:/:/view.changes";

            if (!string.IsNullOrEmpty(token))
            {
                url += "?token=" + token;
            }

            string accessToken = await credentials.GetAccessToken(cancellationToken);

            return await CallViewChanges(url, accessToken, cancellationToken);
        }

        private async Task<ViewChangesResult> CallViewChanges(string url, string accessToken, CancellationToken cancellationToken)
        {
            var response = await GetRawRequest(url, accessToken, cancellationToken);

            using (var streamReader = new StreamReader(response))
            {
                var json = await streamReader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<ViewChangesResult>(json);
            }
        }
    }
}
