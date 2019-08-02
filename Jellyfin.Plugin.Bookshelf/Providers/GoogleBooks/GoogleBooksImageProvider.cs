using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    class GoogleBooksImageProvider : BaseMetadataProvider
    {
        private static IHttpClient _httpClient;
        private static IJsonSerializer _jsonSerializer;
        private static ILogger _logger;
        private static IProviderManager _providerManager;

        public GoogleBooksImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IHttpClient httpClient, IJsonSerializer jsonSerializer, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _logger = logManager.GetLogger("MB Bookshelf");
            _providerManager = providerManager;
        }

        public override bool Supports(BaseItem item)
        {
            return item is Book;
        }

        private async Task<bool> Fetch(BaseItem item, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            //var googleBookId = item.GetProviderId("GoogleBooks");

            //if (string.IsNullOrEmpty(googleBookId))
            //    return false;

            //var bookResult = await FetchBookData(googleBookId, cancellationToken);

            //if (bookResult == null)
            //    return false;

            //await ProcessBookImage(item, bookResult, cancellationToken);

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
        }

        private async Task<BookResult> FetchBookData(string googleBookId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.Format(GoogleApiUrls.DetailsUrl, googleBookId);

            var stream = await _httpClient.Get(url, Plugin.Instance.GoogleBooksSemiphore, cancellationToken);

            if (stream == null)
            {
                _logger.Info("response is null");
                return null;
            }

            return _jsonSerializer.DeserializeFromStream<BookResult>(stream);
        }

        private async Task ProcessBookImage(BaseItem item, BookResult bookResult, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string imageUrl = null;

            if (!string.IsNullOrEmpty(bookResult.volumeInfo.imageLinks.large))
                imageUrl = bookResult.volumeInfo.imageLinks.large;
            else if (!string.IsNullOrEmpty(bookResult.volumeInfo.imageLinks.medium))
                imageUrl = bookResult.volumeInfo.imageLinks.medium;
            else if (!string.IsNullOrEmpty(bookResult.volumeInfo.imageLinks.small))
                imageUrl = bookResult.volumeInfo.imageLinks.small;

            if (!string.IsNullOrEmpty(imageUrl))
                await _providerManager.SaveImage(item, bookResult.volumeInfo.imageLinks.large,
                    Plugin.Instance.GoogleBooksSemiphore, ImageType.Primary, null,
                    cancellationToken).ConfigureAwait(false);
        }
    }
}
