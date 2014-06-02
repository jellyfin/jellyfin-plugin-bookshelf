using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Twitch
{
    public class TwitchChannel : IChannel, IRequiresMediaInfoCallback
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

        public string DataVersion
        {
            get
            {
                // Increment as needed to invalidate all caches
                return "2";
            }
        }

        public async Task<IEnumerable<ChannelItemInfo>> Search(ChannelSearchInfo searchInfo, User user, CancellationToken cancellationToken)
        {
            return null;
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            ChannelItemResult result;

            _logger.Debug("cat ID : " + query.FolderId);

            if (query.FolderId == null)
            {
                result = await GetChannelsInternal(query, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                result = await GetChannelItemsInternal(query, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        private async Task<ChannelItemResult> GetChannelsInternal(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var offset = query.StartIndex.GetValueOrDefault();
            var downloader = new TwitchChannelDownloader(_logger, _jsonSerializer, _httpClient);
            var channels = await downloader.GetTwitchChannelList(offset, cancellationToken);

            var items = channels.top.OrderByDescending(x => x.viewers)
                .Select(i => new ChannelItemInfo
            {
                Type = ChannelItemType.Folder,
                ImageUrl = i.game.box.large,
                Name = i.game.name,
                Id = i.game.name,
                CommunityRating = Convert.ToSingle(i.viewers),
            });

            return new ChannelItemResult
            {
                Items = items.ToList(),
                TotalRecordCount = channels._total,
                CacheLength = TimeSpan.FromDays(3)
            };
        }

        private async Task<ChannelItemResult> GetChannelItemsInternal(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var offset = query.StartIndex.GetValueOrDefault();
            var downloader = new TwitchListingDownloader(_logger, _jsonSerializer, _httpClient);
            var videos = await downloader.GetStreamList(query.FolderId, offset, cancellationToken)
                .ConfigureAwait(false);

            var items = videos.streams.OrderByDescending(x => x.viewers)
                .Select(i => new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.Clip,
                ImageUrl = i.preview.large,
                IsInfiniteStream = true,
                MediaType = ChannelMediaType.Video,
                Name = i.channel.name,
                Id = i.channel.name,
                Type = ChannelItemType.Media,
                CommunityRating = Convert.ToSingle(i.viewers),
                DateCreated = !String.IsNullOrEmpty(i.channel.created_at) ? 
                    Convert.ToDateTime(i.channel.created_at) : (DateTime?)null,
                Overview = i.channel.status,
            });

            return new ChannelItemResult
            {
                Items = items.ToList(),
                TotalRecordCount = videos._total,
                CacheLength = TimeSpan.FromDays(3)
            };
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            using (var json = await _httpClient.Get("http://api.twitch.tv/api/channels/" + id + "/access_token", cancellationToken).ConfigureAwait(false))
            {
                var r = _jsonSerializer.DeserializeFromStream<RootObject>(json);

                var token = WebUtility.UrlEncode(r.token);

                var playURL = "http://usher.twitch.tv/api/channel/hls/" + id + ".m3u8?token=" + token + "&sig=" +
                                    r.sig;

                return new List<ChannelMediaInfo>
                {
                    new ChannelMediaInfo
                    {
                        Path = playURL
                    }
                };
            }
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Thumb:
                case ImageType.Backdrop:
                    {
                        var path = GetType().Namespace + ".Images." + type.ToString().ToLower() + ".png";

                        return Task.FromResult(new DynamicImageResponse
                        {
                            Format = ImageFormat.Png,
                            HasImage = true,
                            Stream = GetType().Assembly.GetManifestResourceStream(path)
                        });
                    }
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
                ImageType.Backdrop
            };
        }

        public string Name
        {
            get { return "Twitch TV"; }
        }



        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                CanSearch = false,

                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Clip
                },

                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Video
                },

                // https://github.com/justintv/Twitch-API/blob/master/v3_resources/streams.md
                MaxPageSize = 100,

                DefaultSortFields = new List<ChannelItemSortField>
                {
                    ChannelItemSortField.CommunityRating,
                },
            };
        }

        public bool IsEnabledFor(User user)
        {
            return true;
        }

        public string HomePageUrl
        {
            get { return "http://www.twitch.tv/"; }
        }


        public Task<ChannelItemResult> GetAllMedia(InternalAllChannelMediaQuery query, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
