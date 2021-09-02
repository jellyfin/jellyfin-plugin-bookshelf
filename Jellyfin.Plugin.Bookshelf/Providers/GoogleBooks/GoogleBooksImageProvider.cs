using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Json;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    public class GoogleBooksImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private ILogger<GoogleBooksImageProvider> _logger;

        public GoogleBooksImageProvider(ILogger<GoogleBooksImageProvider> logger, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public string Name => "Google Books";

        public bool Supports(BaseItem item)
        {
            return item is Book;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var list = new List<RemoteImageInfo>();

            var googleBookId = item.GetProviderId("GoogleBooks");

            if (string.IsNullOrEmpty(googleBookId))
            {
                return list;
            }

            var bookResult = await FetchBookData(googleBookId, cancellationToken);

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

        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            return await httpClient.GetAsync(url).ConfigureAwait(false);
        }

        private async Task<BookResult> FetchBookData(string googleBookId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.Format(GoogleApiUrls.DetailsUrl, googleBookId);

            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);

            using (var response = await httpClient.GetAsync(url).ConfigureAwait(false))
            {
                await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return await JsonSerializer.DeserializeAsync<BookResult>(stream, JsonDefaults.GetOptions()).ConfigureAwait(false);
            }
        }

        private List<string> ProcessBookImage(BookResult bookResult)
        {
            var images = new List<string>();
            if (!string.IsNullOrEmpty(bookResult.volumeInfo.imageLinks?.extraLarge))
            {
                images.Add(bookResult.volumeInfo.imageLinks.extraLarge);
            }
            else if (!string.IsNullOrEmpty(bookResult.volumeInfo.imageLinks?.large))
            {
                images.Add(bookResult.volumeInfo.imageLinks.large);
            }
            else if (!string.IsNullOrEmpty(bookResult.volumeInfo.imageLinks?.medium))
            {
                images.Add(bookResult.volumeInfo.imageLinks.medium);
            }
            else if (!string.IsNullOrEmpty(bookResult.volumeInfo.imageLinks?.small))
            {
                images.Add(bookResult.volumeInfo.imageLinks.small);
            }

            // sometimes the thumbnails can be different from the larger images
            if (!string.IsNullOrEmpty(bookResult.volumeInfo.imageLinks?.thumbnail))
            {
                images.Add(bookResult.volumeInfo.imageLinks.thumbnail);
            }
            else if (!string.IsNullOrEmpty(bookResult.volumeInfo.imageLinks?.smallThumbnail))
            {
                images.Add(bookResult.volumeInfo.imageLinks.smallThumbnail);
            }

            return images;
        }
    }
}
