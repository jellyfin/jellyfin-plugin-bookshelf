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
using Vimeo.API;

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

        public Task<IEnumerable<ChannelItemInfo>> Search(ChannelSearchInfo searchInfo, Controller.Entities.User user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            IEnumerable<ChannelItemInfo> items;
            
            if (query.CategoryId == null)
            {
                items = await GetChannels(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                 items = await GetChannelItems(cancellationToken).ConfigureAwait(false);
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
                Id = i.id.GetMD5().ToString("N"),
                Overview = i.description
            });
        }

        private async Task<IEnumerable<ChannelItemInfo>> GetChannelItems(CancellationToken cancellationToken)
        {
            var downloader = new VimeoListingDownloader(_logger, _jsonSerializer, _httpClient);
            var videos = await downloader.GetVimeoList(cancellationToken)
                .ConfigureAwait(false);

            return videos.Select(i => new ChannelItemInfo
            {
                ContentType = ChannelMediaContentType.Clip,
                ImageUrl = i.Thumbnail,
                IsInfiniteStream = false,
                MediaType = ChannelMediaType.Video,
                Name = i.Name,
                Overview = i.Description,
                Type = ChannelItemType.Media,
                Id = i.URL.GetMD5().ToString("N"),
                PremiereDate = i.UploadDate,

                MediaSources = new List<ChannelMediaInfo>
                {
                    new ChannelMediaInfo
                    {
                         Path = i.URL,
                         VideoBitrate = i.VideoBitRate,
                         Height = i.VideoHeight,
                         Width = i.VideoWidth
                    }
                }
            });
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
