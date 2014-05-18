using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.Twitch
{
    class TwitchChannelDownloader
    {
        private readonly ILogger _logger;
        private IHttpClient _httpClient;
        private IJsonSerializer _jsonSerializer;

        public TwitchChannelDownloader(ILogger logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public async Task<RootObject> GetTwitchChannelList(CancellationToken cancellationToken)
        {
            RootObject reg;

            using (var json = await _httpClient.Get("https://api.twitch.tv/kraken/games/top?limit=100", CancellationToken.None).ConfigureAwait(false))
            {
                reg = _jsonSerializer.DeserializeFromStream<RootObject>(json);
            }

            return reg;
        }

    }
}
