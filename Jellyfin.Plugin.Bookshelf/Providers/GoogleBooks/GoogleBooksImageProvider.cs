using System.Collections.Generic;
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
        private static IHttpClient _httpClient;
        private static IJsonSerializer _jsonSerializer;
        private static ILogger _logger;

        public GoogleBooksImageProvider(ILogger logger, IHttpClient httpClient, IJsonSerializer jsonSerializer)
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
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var list = new List<RemoteImageInfo>();

            var googleBookId = item.GetProviderId("GoogleBooks");

            if (string.IsNullOrEmpty(googleBookId))
                return list;

            var bookResult = await FetchBookData(googleBookId, cancellationToken);

            if (bookResult == null)
                return list;

            list.Add(new RemoteImageInfo
            {
                ProviderName = Name,
                Url = ProcessBookImage(bookResult)
            });

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

        private string ProcessBookImage(BookResult bookResult)
        {
            string imageUrl = null;
            if (!string.IsNullOrEmpty(bookResult.volumeInfo.imageLinks.large))
                imageUrl = bookResult.volumeInfo.imageLinks.large;
            else if (!string.IsNullOrEmpty(bookResult.volumeInfo.imageLinks.medium))
                imageUrl = bookResult.volumeInfo.imageLinks.medium;
            else if (!string.IsNullOrEmpty(bookResult.volumeInfo.imageLinks.small))
                imageUrl = bookResult.volumeInfo.imageLinks.small;
            return imageUrl;
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
