using System;
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
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public TwitchChannelDownloader(ILogger logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public async Task<RootObject> GetTwitchChannelList(int offset, CancellationToken cancellationToken)
        {
            RootObject reg;

            using (var json = await _httpClient.Get(String.Format("https://api.twitch.tv/kraken/games/top?limit=100&offset={0}", offset), CancellationToken.None).ConfigureAwait(false))
            {
                reg = _jsonSerializer.DeserializeFromStream<RootObject>(json);
            }

            return reg;
        }

    }
}
