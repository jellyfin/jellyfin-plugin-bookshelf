using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.UStream
{
    class UStreamChannelDownloader
    {
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public UStreamChannelDownloader(ILogger logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public async Task<RootObject> GetRevision3ChannelList(CancellationToken cancellationToken)
        {
            return null;
        }

    }
}
