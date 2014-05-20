using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Vimeo
{
    public class VimeoChannel : IChannel, IRequiresMediaInfoCallback
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public VimeoChannel(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
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
                return "4";
            }
        }

        public async Task<IEnumerable<ChannelItemInfo>> Search(ChannelSearchInfo searchInfo, Controller.Entities.User user, CancellationToken cancellationToken)
        {
            var downloader = new VimeoListingDownloader(_logger, _jsonSerializer, _httpClient);
            var search = await downloader.GetSearchVimeoList(searchInfo.SearchTerm, cancellationToken);

            return search.Select(i => new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.Clip,
                ImageUrl = i.thumbnails[0].Url,
                IsInfiniteStream = false,
                MediaType = ChannelMediaType.Video,
                Name = i.title,
                Overview = i.description,
                Type = ChannelItemType.Media,
                Id = i.urls[0].Value.GetMD5().ToString("N"),

                MediaSources = new List<ChannelMediaInfo>
                {
                    new ChannelMediaInfo
                    {
                         Path = i.urls[0].Value,
                         Height = i.height,
                         Width = i.width
                    }
                }
            });
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(query.CategoryId))
            {
                return await GetCategories(query, cancellationToken).ConfigureAwait(false);
            }

            var catSplit = query.CategoryId.Split('_');

            if (catSplit[0] == "cat")
            {
                return await GetSubCategories(query, cancellationToken).ConfigureAwait(false);
            } 
            
            if (catSplit[0] == "subcat")
            {
                return await GetChannels(query, cancellationToken).ConfigureAwait(false);
            }

            return await GetChannelItemsInternal(query, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ChannelItemResult> GetCategories(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var downloader = new VimeoCategoryDownloader(_logger, _jsonSerializer, _httpClient);
            var channels = await downloader.GetVimeoCategoryList(query.StartIndex, query.Limit, cancellationToken);



            var items = channels.Select(i => new ChannelItemInfo
            {
                Type = ChannelItemType.Category,
                ImageUrl = i.image,
                Name = i.name,
                Id = "cat_" + i.id,
            }).ToList();

            return new ChannelItemResult
            {
                Items = items,
                CacheLength = TimeSpan.FromDays(1),
                TotalRecordCount = channels.total
            };
        }

        private async Task<ChannelItemResult> GetSubCategories(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var downloader = new VimeoCategoryDownloader(_logger, _jsonSerializer, _httpClient);
            var channels = await downloader.GetVimeoSubCategory(query.CategoryId, cancellationToken);

            var items = channels.subCategories.Select(i => new ChannelItemInfo
            {
                Type = ChannelItemType.Category,
                //ImageUrl = i,
                Name = i.name,
                Id = "subcat_" + i.id,
            }).ToList();

            return new ChannelItemResult
            {
                Items = items,
                CacheLength = TimeSpan.FromDays(1),
            };
        }

        private async Task<ChannelItemResult> GetChannels(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var downloader = new VimeoChannelDownloader(_logger, _jsonSerializer, _httpClient);
            var channels = await downloader.GetVimeoChannelList(query.StartIndex, query.Limit, cancellationToken);

            var items = channels.Select(i => new ChannelItemInfo
            {
                Type = ChannelItemType.Category,
                ImageUrl = i.logo_url,
                Name = i.name,
                Id = "chan_" + i.id,
                Overview = i.description
            }).ToList();

            return new ChannelItemResult
            {
                Items = items,
                CacheLength = TimeSpan.FromDays(1),
                TotalRecordCount = channels.total
            };
        }

        private async Task<ChannelItemResult> GetChannelItemsInternal(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var downloader = new VimeoListingDownloader(_logger, _jsonSerializer, _httpClient);
            var videos = await downloader.GetVimeoList(query.CategoryId, query.StartIndex, query.Limit, cancellationToken)
                .ConfigureAwait(false);

            var items = videos.Select(i => new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.Clip,
                ImageUrl = i.thumbnails[2].Url,
                IsInfiniteStream = false,
                MediaType = ChannelMediaType.Video,
                Name = i.title,
                Overview = i.description,
                Type = ChannelItemType.Media,
                Id = i.id,
                RunTimeTicks = TimeSpan.FromSeconds(i.duration).Ticks,
                //Tags = i.tags == null ? new List<string>() : i.tags.Select(t => t.title).ToList()

            });

            return new ChannelItemResult
            {
                Items = items.ToList(),
                CacheLength = TimeSpan.FromDays(1),
                TotalRecordCount = videos.total
            };
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
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
                case ImageType.Backdrop:
                case ImageType.Thumb:
                    {
                        var path = GetType().Namespace + ".Images." + type.ToString().ToLower() + ".png";

                        return Task.FromResult(new DynamicImageResponse
                        {
                            Format = ImageFormat.Png,
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
            get { return "Vimeo"; }
        }



        public ChannelInfo GetChannelInfo()
        {
            return new ChannelInfo
            {
                CanSearch = true,
                MaxPageSize = 50,

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

        public bool IsEnabledFor(Controller.Entities.User user)
        {
            return true;
        }

        public async Task<IEnumerable<ChannelMediaInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            using (var json = await _httpClient.Get(
                "http://player.vimeo.com/v2/video/" + id +
                "/config?autoplay=0&byline=0&bypass_privacy=1&context=clip.main&default_to_hd=1&portrait=0&title=0",
                CancellationToken.None).ConfigureAwait(false))
            {
                var r = _jsonSerializer.DeserializeFromStream<RootObject>(json);

                var mediaInfo = new List<ChannelMediaInfo>();


                if (r.request != null && r.request.files != null)
                {
                    if (r.request.files.h264 != null)
                    {

                        var hd = r.request.files.h264.hd;
                        if (hd != null && !string.IsNullOrEmpty(hd.url))
                        {
                            mediaInfo.Add(
                                new ChannelMediaInfo
                                {
                                    Height = hd.height,
                                    Width = hd.width,
                                    Path = hd.url
                                }
                            );
                        }

                        var sd = r.request.files.h264.sd;
                        if (sd != null && !string.IsNullOrEmpty(sd.url))
                        {
                            mediaInfo.Add(
                                new ChannelMediaInfo
                                {
                                    Height = sd.height,
                                    Width = sd.width,
                                    Path = sd.url
                                }
                             );
                        }

                        var mob = r.request.files.h264.sd;
                        if (mob != null && !string.IsNullOrEmpty(mob.url))
                        {
                            mediaInfo.Add(
                                new ChannelMediaInfo
                                {
                                    Height = mob.height,
                                    Width = mob.width,
                                    Path = mob.url
                                }
                             );
                        }
                    }
                }

                return mediaInfo;
            }
        }
    }
}
