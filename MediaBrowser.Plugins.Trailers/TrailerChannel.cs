using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers
{
    public class TrailerChannel : IChannel
    {
        private readonly IHttpClient _httpClient;

        public TrailerChannel(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public ChannelCapabilities GetCapabilities()
        {
            return new ChannelCapabilities
            {
                CanSearch = false
            };
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var items = await GetChannelItems(cancellationToken).ConfigureAwait(false);

            return new ChannelItemResult
            {
                Items = items.ToList(),
                CacheLength = TimeSpan.FromDays(3)
            };
        }

        private async Task<IEnumerable<ChannelItemInfo>> GetChannelItems(CancellationToken cancellationToken)
        {
            var trailers = await AppleTrailerListingDownloader.GetTrailerList(_httpClient, cancellationToken)
                .ConfigureAwait(false);

            return trailers.Select(i => new ChannelItemInfo
            {
                CommunityRating = i.Video.CommunityRating,
                ContentType = ChannelMediaContentType.Trailer,
                Genres = i.Video.Genres,
                ImageUrl = i.HdImageUrl ?? i.ImageUrl,
                IsInfiniteStream = false,
                MediaType = ChannelMediaType.Video,
                Name = i.Video.Name,
                OfficialRating = i.Video.OfficialRating,
                Overview = i.Video.Overview,
                People = i.Video.People,
                ProviderIds = i.Video.ProviderIds,
                Type = ChannelItemType.Media,
                Id = i.TrailerUrl.GetMD5().ToString("N")
            });
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType> { };
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
    }
}
