using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace Dropbox.Api
{
    public class DropboxContentApi : ApiService, IDropboxContentApi
    {
        public DropboxContentApi(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationHost applicationHost)
            : base(httpClient, jsonSerializer, applicationHost)
        { }

        protected override string BaseUrl
        {
            get { return "https://api-content.dropbox.com/1/"; }
        }

        public async Task<ChunkedUploadResult> ChunkedUpload(string uploadId, byte[] content, int offset, string accessToken, CancellationToken cancellationToken)
        {
            var url = string.Format("/chunked_upload?upload_id={0}&offset={1}", uploadId, offset);

            return await PutRequest<ChunkedUploadResult>(url, accessToken, content, cancellationToken);
        }

        public async Task CommitChunkedUpload(string path, string uploadId, string accessToken, CancellationToken cancellationToken)
        {
            var url = "/commit_chunked_upload/auto/" + path;
            var data = new Dictionary<string, string>
            {
                { "overwrite", "true" },
                { "upload_id", uploadId }
            };

            await PostRequest<object>(url, accessToken, data, cancellationToken);
        }
    }
}
