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
        private const string FeedUrl = "http://vimeo.com/api/v2/brad/videos.xml?callback=showThumbs";

        /// <summary>
        /// Downloads a list of trailer info's from the apple url
        /// </summary>
        /// <returns>Task{List{TrailerInfo}}.</returns>
        public async Task<List<VimeoInfo>> GetVimeoList(CancellationToken cancellationToken)
        {
            var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = FeedUrl,
                CancellationToken = cancellationToken,
                UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/30.0.1599.28 Safari/537.36"

            }).ConfigureAwait(false);

            var list = new List<VimeoInfo>();

            using (var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true }))
            {
                await reader.MoveToContentAsync().ConfigureAwait(false);

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.Name)
                        {
                            case "video":
                            {
                                var trailer = FetchInfo(reader.ReadSubtree());
                                if (trailer.Privacy == "anywhere")
                                {
                                    await GetUrl(trailer);
                                    list.Add(trailer);
                                }
                                break;
                            }
                        }
                    }
                }
            }

            return list;
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

        private async Task<String> GetUrl(VimeoInfo i)
        {
            var reg = new RootObject();

            using (var json = await _httpClient.Get("http://player.vimeo.com/v2/video/" + i.ID + "/config?autoplay=0&byline=0&bypass_privacy=1&context=clip.main&default_to_hd=1&portrait=0&title=0", CancellationToken.None).ConfigureAwait(false))
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
                i.VideoWidth = SD.width;
                i.VideoHeight = SD.height;
                i.VideoBitRate = SD.bitrate;
                i.URL = SD.url;
            //}

            return i.URL;
        }
    }
}
