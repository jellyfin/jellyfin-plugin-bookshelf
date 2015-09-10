using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MBBookshelf.Providers.GoogleBooks
{
    class GoogleBooksImageProvider : BaseMetadataProvider
    {

        private static IHttpClient _httpClient;
        private static IJsonSerializer _jsonSerializer;
        private static ILogger _logger;
        private static IProviderManager _providerManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logManager"></param>
        /// <param name="configurationManager"></param>
        /// <param name="httpClient"></param>
        /// <param name="jsonSerializer"></param>
        /// <param name="providerManager"></param>
        public GoogleBooksImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IHttpClient httpClient, IJsonSerializer jsonSerializer, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _logger = logManager.GetLogger("MB Bookshelf");
            _providerManager = providerManager;
        }

        #region BaseMetadataProvider

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override bool Supports(BaseItem item)
        {
            return item is Book;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="force"></param>
        /// <param name="providerInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            await Fetch(item, providerInfo, cancellationToken).ConfigureAwait(false);

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Third; }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool RequiresInternet
        {
            get { return true; }
        }

        /// <summary>
        /// 
        /// </summary>
        public override ItemUpdateType ItemUpdateType
        {
            get
            {
                return ItemUpdateType.ImageUpdate;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override string ProviderVersion
        {
            get
            {
                return "GoogleBooks Image Provider version 1.00";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override bool RefreshOnVersionChange
        {
            get { return true; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="providerInfo"></param>
        /// <returns></returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            //if (string.IsNullOrEmpty(item.GetProviderId("GoogleBooks")))
            //    return false;

            //return base.NeedsRefreshInternal(item, providerInfo);
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected override DateTime CompareDate(BaseItem item)
        {
            return item.DateModified;
        }

        #endregion
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="googleBookId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="bookResult"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
                await
                    _providerManager.SaveImage(item, bookResult.volumeInfo.imageLinks.large,
                                               Plugin.Instance.GoogleBooksSemiphore, ImageType.Primary, null,
                                               cancellationToken).ConfigureAwait(false);
            

        }
    }
}
