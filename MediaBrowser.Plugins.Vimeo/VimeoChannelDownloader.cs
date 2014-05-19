using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.Vimeo.VimeoAPI.API;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Vimeo
{
    class VimeoChannelDownloader
    {
        private readonly ILogger _logger;
        private IHttpClient _httpClient;
        private IJsonSerializer _jsonSerializer;

        public VimeoChannelDownloader(ILogger logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public async Task<Channels> GetVimeoChannelList(int? startIndex, int? limit, CancellationToken cancellationToken)
        {
            int? page = null;

            if (startIndex.HasValue && limit.HasValue)
            {
                page = 1 + (startIndex.Value / limit.Value) % limit.Value;
            }

            var channels = Plugin.vc.vimeo_channels_getAll(page: page, per_page: limit);
            return channels;
        }

    }
}
