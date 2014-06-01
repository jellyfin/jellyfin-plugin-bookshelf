using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.Vimeo.VimeoAPI.API;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Vimeo
{
    public class VimeoListingDownloader
    {
        private ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public VimeoListingDownloader(ILogger logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public async Task<Videos> GetVimeoList(String catID, int? startIndex, int? limit, CancellationToken cancellationToken)
        {
            int? page = null;

            if (startIndex.HasValue && limit.HasValue)
            {
                page = 1 + (startIndex.Value / limit.Value) % limit.Value;
            }

            var videos = Plugin.vc.vimeo_channels_getVideos(catID, true, page: page, per_page: limit);

            foreach (var vid in videos.ToList().Where(vid => vid.embed_privacy != "anywhere"))
            {
                videos.Remove(vid);
            }

            return videos;
        }

        public async Task<Videos> GetCategoryVideoList(String catID, int? startIndex, int? limit, CancellationToken cancellationToken)
        {
            int? page = null;

            if (startIndex.HasValue && limit.HasValue)
            {
                page = 1 + (startIndex.Value / limit.Value) % limit.Value;
            }

            var videos = Plugin.vc.vimeo_categories_getRelatedVideos(catID, true, page: page, per_page: limit);

            foreach (var vid in videos.ToList().Where(vid => vid.embed_privacy != "anywhere"))
            {
                videos.Remove(vid);
            }

            return videos;
        }

        public async Task<Videos> GetSearchVimeoList(String searchTerm, CancellationToken cancellationToken)
        {
            var search = Plugin.vc.vimeo_videos_search(true, null, null, searchTerm, VimeoClient.VideosSortMethod.Default,
                null);

            foreach (var s in search.ToList().Where(s => s.embed_privacy != "anywhere"))
            {
                search.Remove(s);
            }

            return search;
        }
    }

}
