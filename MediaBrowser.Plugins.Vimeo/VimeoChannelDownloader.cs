using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
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

        public async Task<Channels> GetVimeoChannelList(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            int? page = null;

            if (query.StartIndex.HasValue && query.Limit.HasValue)
            {
                page = 1 + (query.StartIndex.Value / query.Limit.Value) % query.Limit.Value;
            }

            var channels = Plugin.vc.vimeo_categories_getRelatedChannels(query.CategoryId,page: page, per_page: query.Limit);
            return channels;
        }

    }
}
