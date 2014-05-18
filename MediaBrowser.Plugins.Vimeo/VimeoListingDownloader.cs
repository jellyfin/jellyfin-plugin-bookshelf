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

            foreach (var vid in videos)
            {
                await GetUrl(vid);
            }

            return videos;
        }
        
        public async Task<Videos> GetSearchVimeoList(String searchTerm, CancellationToken cancellationToken)
        {
            Videos search = Plugin.vc.vimeo_videos_search(true, null, null, searchTerm, VimeoClient.VideosSortMethod.Default,
                null);

            foreach (var s in search)
            {
                await GetUrl(s);
            }

            return search;
        }

        private async Task GetUrl(VimeoAPI.API.Video v)
        {
            RootObject reg;

            using (var json = await _httpClient.Get("http://player.vimeo.com/v2/video/" + v.id + "/config?autoplay=0&byline=0&bypass_privacy=1&context=clip.main&default_to_hd=1&portrait=0&title=0", CancellationToken.None).ConfigureAwait(false))
            {
                reg = _jsonSerializer.DeserializeFromStream<RootObject>(json);
            }

            if (reg.request.files.h264 != null)
            {
                if (v.is_hd)
                {
                    var hd = reg.request.files.h264.hd;
                    if (hd != null)
                    {
                        /*
                        _logger.Debug("Name : " + v.title);
                        _logger.Debug("Description : " + v.description);
                        _logger.Debug("Upload d : " + v.upload_date);
                        _logger.Debug("Mod d : " + v.modified_date);
                        _logger.Debug("likes : " + v.number_of_likes);
                        _logger.Debug("comments : " + v.number_of_comments);
                        _logger.Debug("Width : " + v.width);
                        _logger.Debug("Owner : " + v.owner);
                        _logger.Debug("Thumb : " + v.thumbnails[0].Url);

                        _logger.Debug("HD URL " + hd.url);*/
                        v.width = hd.width;
                        v.height = hd.height;
                        v.urls.Add(new VimeoAPI.API.Video.Url
                        {
                            type = "player",
                            Value = hd.url
                        });
                    }
                }
                else
                {
                    var sd = reg.request.files.h264.sd;
                    if (sd != null)
                    {
                        v.width = sd.width;
                        v.height = sd.height;
                        v.urls.Add(new VimeoAPI.API.Video.Url
                        {
                            type = "player",
                            Value = sd.url
                        });
                    }
                }
            }
        }
    }
}
