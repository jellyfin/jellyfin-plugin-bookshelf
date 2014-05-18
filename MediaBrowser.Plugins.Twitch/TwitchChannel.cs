using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MediaBrowser.Plugins.Twitch
{
    public class TwitchChannel : IChannel
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public TwitchChannel(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            _httpClient = httpClient;
            _logger = logManager.GetLogger(GetType().Name);
            _jsonSerializer = jsonSerializer;
        }

        public async Task<IEnumerable<ChannelItemInfo>> Search(ChannelSearchInfo searchInfo, Controller.Entities.User user, CancellationToken cancellationToken)
        {
            return null;
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            IEnumerable<ChannelItemInfo> items;

            _logger.Debug("cat ID : " + query.CategoryId);

            if (query.CategoryId == null)
            {
                items = await GetChannels(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                 items = await GetChannelItems(query.CategoryId, cancellationToken).ConfigureAwait(false);
            }

            return new ChannelItemResult
            {
                Items = items.ToList()//,
                //CacheLength = TimeSpan.FromDays(3)
            };
        }

        private async Task<IEnumerable<ChannelItemInfo>> GetChannels(CancellationToken cancellationToken)
        {
            var downloader = new TwitchChannelDownloader(_logger, _jsonSerializer, _httpClient);
            var channels = await downloader.GetTwitchChannelList(cancellationToken);
           
            return channels.top.Select(i => new ChannelItemInfo
            {
                Type = ChannelItemType.Category,
                ImageUrl = i.game.box.large,
                Name = i.game.name,
                Id = i.game.name
            });
        }

        private async Task<IEnumerable<ChannelItemInfo>> GetChannelItems(String catID, CancellationToken cancellationToken)
        {
            var downloader = new TwitchListingDownloader(_logger, _jsonSerializer, _httpClient);
            var videos = await downloader.GetStreamList(catID, cancellationToken)
                .ConfigureAwait(false);

            foreach (var v in videos.streams.ToList())
            {
                await getURL(videos, v);
            }

            return videos.streams.Select(i => new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.Clip,
                ImageUrl = i.preview.large,
                IsInfiniteStream = true,
                MediaType = ChannelMediaType.Video,
                Name = i.channel.name,
                //Overview = i.channel.,
                Type = ChannelItemType.Media,
                Id = i.channel._id.ToString("N"),
               // PremiereDate = i.upload_date,

                MediaSources = new List<ChannelMediaInfo>
                {
                    new ChannelMediaInfo
                    {
                         Path = i.channel.playURL,
                    }
                }

            });
        }

        private async Task getURL(RootObject r, Stream s)
        {
            using (var json = await _httpClient.Get("http://api.twitch.tv/api/channels/" + s.channel.name + "/access_token", CancellationToken.None).ConfigureAwait(false))
            {
                r = _jsonSerializer.DeserializeFromStream<RootObject>(json);
            }

            var token = HttpUtility.UrlEncode(r.token);

            s.channel.playURL = "http://usher.twitch.tv/api/channel/hls/" + s.channel.name + ".m3u8?token=" + token + "&sig=" +
                                r.sig;
        }
        

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {   
                case ImageType.Thumb:
                case ImageType.Primary:
                    {
                        var path = GetType().Namespace + ".Images." + type.ToString().ToLower() + ".jpg";

                        return Task.FromResult(new DynamicImageResponse
                        {
                            Format = ImageFormat.Jpg,
                            HasImage = true,
                            Stream = GetType().Assembly.GetManifestResourceStream(path)
                        });
                    }
                default:
                    throw new ArgumentException("Unsupported image type: " + type);
            }
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Thumb,
                ImageType.Primary,
            };
        }

        public string Name
        {
            get { return "Twitch TV"; }
        }

    

        public ChannelInfo GetChannelInfo()
        {
            return new ChannelInfo
            {
                CanSearch = false,

                ContentTypes = new List<ChannelMediaContentType>
                 {
                     ChannelMediaContentType.Clip
                 },

                MediaTypes = new List<ChannelMediaType>
                  {
                       ChannelMediaType.Video
                  }
            };
        }

        public bool IsEnabledFor(User user)
        {
            return true;
        }
    }
}
