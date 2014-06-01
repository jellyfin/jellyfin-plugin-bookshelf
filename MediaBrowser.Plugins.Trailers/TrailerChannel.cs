using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers
{
    public class TrailerChannel : IChannel
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public TrailerChannel(IHttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public string DataVersion
        {
            get
            {
                return "8";
            }
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var items = await GetChannelItems(cancellationToken).ConfigureAwait(false);

            if (query.SortBy.HasValue)
            {
                if (query.SortDescending)
                {
                    switch (query.SortBy.Value)
                    {
                        case ChannelItemSortField.Runtime:
                            items = items.OrderByDescending(i => i.RunTimeTicks ?? 0);
                            break;
                        case ChannelItemSortField.PremiereDate:
                            items = items.OrderByDescending(i => i.PremiereDate ?? DateTime.MinValue);
                            break;
                        case ChannelItemSortField.DateCreated:
                            items = items.OrderByDescending(i => i.DateCreated ?? DateTime.MinValue);
                            break;
                        case ChannelItemSortField.CommunityRating:
                            items = items.OrderByDescending(i => i.CommunityRating ?? 0);
                            break;
                        default:
                            items = items.OrderByDescending(i => i.Name);
                            break;
                    }
                }
                else
                {
                    switch (query.SortBy.Value)
                    {
                        case ChannelItemSortField.Runtime:
                            items = items.OrderBy(i => i.RunTimeTicks ?? 0);
                            break;
                        case ChannelItemSortField.PremiereDate:
                            items = items.OrderBy(i => i.PremiereDate ?? DateTime.MinValue);
                            break;
                        case ChannelItemSortField.DateCreated:
                            items = items.OrderBy(i => i.DateCreated ?? DateTime.MinValue);
                            break;
                        case ChannelItemSortField.CommunityRating:
                            items = items.OrderBy(i => i.CommunityRating ?? 0);
                            break;
                        default:
                            items = items.OrderBy(i => i.Name);
                            break;
                    }
                }
            }

            return new ChannelItemResult
            {
                Items = items.ToList(),
                CacheLength = TimeSpan.FromDays(3)
            };
        }

        private async Task<IEnumerable<ChannelItemInfo>> GetChannelItems(CancellationToken cancellationToken)
        {
            var hdTrailers = await AppleTrailerListingDownloader.GetTrailerList(_httpClient,
                _logger,
                true,
                cancellationToken)
                .ConfigureAwait(false);

            var sdTrailers = await AppleTrailerListingDownloader.GetTrailerList(_httpClient,
                _logger,
                false,
                cancellationToken)
                .ConfigureAwait(false);

            var now = DateTime.UtcNow;
            var maxDays = Plugin.Instance.Configuration.MaxTrailerAge;

            var list = new List<ChannelItemInfo>();

            foreach (var i in hdTrailers
                .Where(i => !maxDays.HasValue || (now - i.PostDate).TotalDays <= maxDays.Value))
            {
                // Avoid duplicates
                if (list.Any(l => string.Equals(i.Name, l.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var channelItem = new ChannelItemInfo
                {
                    CommunityRating = i.CommunityRating,
                    ContentType = ChannelMediaContentType.Trailer,
                    Genres = i.Genres,
                    ImageUrl = i.HdImageUrl ?? i.ImageUrl,
                    IsInfiniteStream = false,
                    MediaType = ChannelMediaType.Video,
                    Name = i.Name,
                    OfficialRating = i.OfficialRating,
                    Overview = i.Overview,
                    People = i.People,
                    Type = ChannelItemType.Media,
                    Id = i.TrailerUrl.GetMD5().ToString("N"),
                    PremiereDate = i.PremiereDate,
                    ProductionYear = i.ProductionYear,
                    Studios = i.Studios,
                    RunTimeTicks = i.RunTimeTicks,

                    MediaSources = new List<ChannelMediaInfo>
                    {
                        GetMediaInfo(i, true)
                    }
                };

                var sdVersion = sdTrailers
                    .FirstOrDefault(l => string.Equals(i.Name, l.Name, StringComparison.OrdinalIgnoreCase));

                if (sdVersion != null)
                {
                    channelItem.MediaSources.Add(GetMediaInfo(sdVersion, false));
                }

                list.Add(channelItem);
            }

            return list;
        }

        private ChannelMediaInfo GetMediaInfo(TrailerInfo info, bool isHd)
        {
            return new ChannelMediaInfo
            {
                Path = info.TrailerUrl,
                Width = isHd ? 1280 : 720,
                Height = isHd ? 720 : 480,
                IsRemote = true,
                Container = Path.GetExtension(info.TrailerUrl)
            };
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Primary:
                case ImageType.Thumb:
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
                ImageType.Primary
            };
        }

        public string HomePageUrl
        {
            get { return "http://mediabrowser3.com"; }
        }

        public bool IsEnabledFor(User user)
        {
            return true;
        }

        public string Name
        {
            get { return "Trailers"; }
        }

        public Task<IEnumerable<ChannelItemInfo>> Search(ChannelSearchInfo searchInfo, User user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                CanSearch = false,

                ContentTypes = new List<ChannelMediaContentType>
                 {
                     ChannelMediaContentType.Trailer
                 },

                MediaTypes = new List<ChannelMediaType>
                  {
                       ChannelMediaType.Video
                  },

                SupportsSortOrderToggle = true,

                DefaultSortFields = new List<ChannelItemSortField>
                   {
                        ChannelItemSortField.CommunityRating,
                        ChannelItemSortField.Name,
                        ChannelItemSortField.DateCreated,
                        ChannelItemSortField.PremiereDate,
                        ChannelItemSortField.Runtime
                   }
            };
        }
    }
}
