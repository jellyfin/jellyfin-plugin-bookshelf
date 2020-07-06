using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    public class GoogleBooksImageProvider : IRemoteImageProvider
    {
        private IHttpClient _httpClient;
        private IJsonSerializer _jsonSerializer;
        private ILogger<GoogleBooksImageProvider> _logger;

        public GoogleBooksImageProvider(ILogger<GoogleBooksImageProvider> logger, IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
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

        private async Task<BookResult> FetchBookData(string googleBookId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.Format(GoogleApiUrls.DetailsUrl, googleBookId);

            var stream = await _httpClient.SendAsync(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = true,
                EnableDefaultUserAgent = true
            }, "GET");

            if (stream == null)
            {
                _logger.LogInformation("response is null");
                return null;
            }

            return _jsonSerializer.DeserializeFromStream<BookResult>(stream.Content);
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

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }
    }
}
