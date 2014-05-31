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
            RootObject reg;

            using (var json = await _httpClient.Get("http://revision3.com/api/getShows.json?api_key=0b1faede6785d04b78735b139ddf2910f34ad601&grouping=latest", CancellationToken.None).ConfigureAwait(false))
            {
                reg = _jsonSerializer.DeserializeFromStream<RootObject>(json);
            }

            return reg;
        }

    }
}
