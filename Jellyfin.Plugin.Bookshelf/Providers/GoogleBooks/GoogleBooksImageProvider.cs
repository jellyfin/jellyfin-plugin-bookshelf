using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    /// <summary>
    /// Google books image provider.
    /// </summary>
    public class GoogleBooksImageProvider : BaseGoogleBooksProvider, IRemoteImageProvider
    {
        private readonly ILogger<GoogleBooksImageProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleBooksImageProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{GoogleBooksProvider}"/> interface.</param>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        public GoogleBooksImageProvider(ILogger<GoogleBooksImageProvider> logger, IHttpClientFactory httpClientFactory)
             : base(logger, httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <inheritdoc />
        public string Name => GoogleBooksConstants.ProviderName;

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is Book || item is Audio;
        }

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var list = new List<RemoteImageInfo>();

            var googleBookId = item.GetProviderId(GoogleBooksConstants.ProviderId);

            if (string.IsNullOrEmpty(googleBookId))
            {
                return list;
            }

            var bookResult = await FetchBookData(googleBookId, cancellationToken).ConfigureAwait(false);

            if (bookResult == null)
            {
                return list;
            }

            list.AddRange(ProcessBookImage(bookResult).Select(image => new RemoteImageInfo
            {
                ProviderName = Name,
                Url = image
            }));

            return list;
        }

        private List<string> ProcessBookImage(BookResult bookResult)
        {
            var images = new List<string>();
            if (bookResult.VolumeInfo is null)
            {
                return images;
            }

            if (!string.IsNullOrEmpty(bookResult.VolumeInfo.ImageLinks?.ExtraLarge))
            {
                images.Add(bookResult.VolumeInfo.ImageLinks.ExtraLarge);
            }
            else if (!string.IsNullOrEmpty(bookResult.VolumeInfo.ImageLinks?.Large))
            {
                images.Add(bookResult.VolumeInfo.ImageLinks.Large);
            }
            else if (!string.IsNullOrEmpty(bookResult.VolumeInfo.ImageLinks?.Medium))
            {
                images.Add(bookResult.VolumeInfo.ImageLinks.Medium);
            }
            else if (!string.IsNullOrEmpty(bookResult.VolumeInfo.ImageLinks?.Small))
            {
                images.Add(bookResult.VolumeInfo.ImageLinks.Small);
            }

            // sometimes the thumbnails can be different from the larger images
            if (!string.IsNullOrEmpty(bookResult.VolumeInfo.ImageLinks?.Thumbnail))
            {
                images.Add(bookResult.VolumeInfo.ImageLinks.Thumbnail);
            }
            else if (!string.IsNullOrEmpty(bookResult.VolumeInfo.ImageLinks?.SmallThumbnail))
            {
                images.Add(bookResult.VolumeInfo.ImageLinks.SmallThumbnail);
            }

            return images;
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            return await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        }
    }
}
