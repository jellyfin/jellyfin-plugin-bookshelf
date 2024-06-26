using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    /// <summary>
    /// Comic Vine image provider.
    /// </summary>
    public class ComicVineImageProvider : BaseComicVineProvider, IRemoteImageProvider
    {
        private readonly ILogger<ComicVineImageProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComicVineImageProvider"/> class.
        /// </summary>
        /// <param name="comicVineMetadataCacheManager">Instance of the <see cref="IComicVineMetadataCacheManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{ComicVineImageProvider}"/> interface.</param>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="apiKeyProvider">Instance of the <see cref="IComicVineApiKeyProvider"/> interface.</param>
        public ComicVineImageProvider(
            IComicVineMetadataCacheManager comicVineMetadataCacheManager,
            ILogger<ComicVineImageProvider> logger,
            IHttpClientFactory httpClientFactory,
            IComicVineApiKeyProvider apiKeyProvider)
            : base(logger, comicVineMetadataCacheManager, httpClientFactory, apiKeyProvider)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <inheritdoc/>
        public string Name => ComicVineConstants.ProviderName;

        /// <inheritdoc/>
        public bool Supports(BaseItem item)
        {
            return item is Book;
        }

        /// <inheritdoc/>
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var issueProviderId = item.GetProviderId(ComicVineConstants.ProviderId);

            if (string.IsNullOrWhiteSpace(issueProviderId))
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var issueDetails = await GetOrAddItemDetailsFromCache<IssueDetails>(issueProviderId, cancellationToken).ConfigureAwait(false);

            if (issueDetails == null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var images = ProcessImages(issueDetails.Image)
                .Select(url => new RemoteImageInfo
                {
                    Url = url,
                    ProviderName = ComicVineConstants.ProviderName
                });

            return images;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            return await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        }
    }
}
