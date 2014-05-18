using System.Linq;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.Vimeo.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Plugins.Vimeo.VimeoAPI.API;

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

        public async Task<Videos> GetVimeoList(String catID, CancellationToken cancellationToken)
        {
            var videos = Plugin.vc.vimeo_channels_getVideos(catID, true);

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
