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
    /// <summary>
    /// Fetches Apple's list of current movie trailers
    /// </summary>
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

        /// <summary>
        /// The trailer feed URL
        /// </summary>
       // private const string FeedUrl = "http://vimeo.com/api/v2/brad/videos.xml?callback=showThumbs";

        /// <summary>
        /// Downloads a list of trailer info's from the apple url
        /// </summary>
        /// <returns>Task{List{TrailerInfo}}.</returns>
        public async Task<Videos> GetVimeoList(String catID, CancellationToken cancellationToken)
        {
            var videos = Plugin.vc.vimeo_channels_getVideos(catID, true);

            foreach (var vid in videos)
            {
                GetUrl(vid);
            }

            return videos;
        }

        /// <summary>
        /// Fetches trailer info from an xml node
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>TrailerInfo.</returns>

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Fetches from the info node
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="info">The info.</param>
        private VimeoInfo FetchInfo(XmlReader reader)
        {
            var info = new VimeoInfo { };

            reader.MoveToContent();
            reader.Read();

            while (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "id":
                        info.ID = reader.ReadElementContentAsInt();
                        break;
                    case "title":
                        info.Name = reader.ReadStringSafe();
                        break;
                    case "description":
                        info.Description = reader.ReadStringSafe();
                        break;
                    case "thumbnail_medium":
                        info.Thumbnail = reader.ReadStringSafe();
                        break;
                    case "embed_privacy":
                        info.Privacy = reader.ReadStringSafe();
                        break;
                    case "upload_date":
                        {
                            DateTime date;

                            if (DateTime.TryParse(reader.ReadStringSafe(), UsCulture, DateTimeStyles.None, out date))
                            {
                                info.UploadDate = date.ToUniversalTime();
                            }
                            break;
                        }

                    default:
                        reader.Skip();
                        break;
                }
            }
            return info;
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

        private async Task<String> GetUrl(VimeoAPI.API.Video v)
        {
            var reg = new RootObject();

            using (var json = await _httpClient.Get("http://player.vimeo.com/v2/video/" + v.id + "/config?autoplay=0&byline=0&bypass_privacy=1&context=clip.main&default_to_hd=1&portrait=0&title=0", CancellationToken.None).ConfigureAwait(false))
            {
                reg = _jsonSerializer.DeserializeFromStream<RootObject>(json);
            }



            //var HD = reg.request.files.h264.hd;
            var SD = reg.request.files.h264.sd;

            /*if (HD.url == null)
            {
                i.VideoWidth = HD.width;
                i.VideoHeight = HD.height;
                i.VideoBitRate = HD.bitrate;
                i.URL = HD.url;
            }
            else
            {*/
            v.width = SD.width;
            v.height = SD.height;
            v.urls[0].Value = SD.url;


            return v.urls[0].Value;
        }
    }
}
