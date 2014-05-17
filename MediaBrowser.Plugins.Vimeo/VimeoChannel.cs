using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;  
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.Vimeo
{
    public class VimeoChannel : IChannel
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
            var downloader = new VimeoChannelDownloader(_logger, _jsonSerializer, _httpClient);
            var channels = await downloader.GetVimeoChannelList(cancellationToken);
           
            return channels.Select(i => new ChannelItemInfo
            {
                Type = ChannelItemType.Category,
                ImageUrl = i.logo_url,
                Name = i.name,
                Id = i.id,
                Overview = i.description
            });
        }

        private async Task<IEnumerable<ChannelItemInfo>> GetChannelItems(String catID, CancellationToken cancellationToken)
        {
            var downloader = new VimeoListingDownloader(_logger, _jsonSerializer, _httpClient);
            var videos = await downloader.GetVimeoList(catID, cancellationToken)
                .ConfigureAwait(false);

            return videos.Select(i => new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.Clip,
                ImageUrl = i.thumbnails[0].Url,
                IsInfiniteStream = false,
                MediaType = ChannelMediaType.Video,
                Name = i.title,
                Overview = i.description,
                Type = ChannelItemType.Media,
                Id = i.id,
               // PremiereDate = i.upload_date,

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

        public Task<IEnumerable<ChannelItemInfo>> Search(ChannelSearchInfo searchInfo, User user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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

        public bool IsEnabledFor(Controller.Entities.User user)
        {
            return true;
        }
    }
}
