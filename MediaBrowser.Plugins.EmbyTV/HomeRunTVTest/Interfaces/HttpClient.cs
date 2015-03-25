using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using System.Net;

namespace HomeRunTVTest.Interfaces
{
    class HttpClient:IHttpClient
    {
        private WebRequest webrequest;
        public HttpClient() { }
        public Task<System.IO.Stream> Get(HttpRequestOptions options)
        {
            string url = options.Url;
            webrequest =   WebRequest.Create(url);
            Task<System.IO.Stream> stream;

                 stream = new Task<System.IO.Stream>(() => webrequest.GetResponse().GetResponseStream());
                stream.Start();
            return stream;
          
        }

        public Task<System.IO.Stream> Get(string url, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<System.IO.Stream> Get(string url, System.Threading.SemaphoreSlim resourcePool, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseInfo> GetResponse(HttpRequestOptions options)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetTempFile(HttpRequestOptions options)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseInfo> GetTempFileResponse(HttpRequestOptions options)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseInfo> Post(HttpRequestOptions options)
        {
            throw new NotImplementedException();
        }

        public Task<System.IO.Stream> Post(HttpRequestOptions options, Dictionary<string, string> postData)
        {
            throw new NotImplementedException();
        }

        public Task<System.IO.Stream> Post(string url, Dictionary<string, string> postData, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<System.IO.Stream> Post(string url, Dictionary<string, string> postData, System.Threading.SemaphoreSlim resourcePool, System.Threading.CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseInfo> SendAsync(HttpRequestOptions options, string httpMethod)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
