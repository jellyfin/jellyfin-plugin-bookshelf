using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    /// <summary>
    /// Comic Vine person metadata provider.
    /// </summary>
    public class ComicVinePersonProvider : BaseComicVineProvider, IRemoteMetadataProvider<Person, PersonLookupInfo>
    {
        private readonly ILogger<ComicVinePersonProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IComicVineApiKeyProvider _apiKeyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComicVinePersonProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{ComicVinePersonProvider}"/> interface.</param>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="comicVineMetadataCacheManager">Instance of the <see cref="IComicVineMetadataCacheManager"/> interface.</param>
        /// <param name="apiKeyProvider">Instance of the <see cref="IComicVineApiKeyProvider"/> interface.</param>
        public ComicVinePersonProvider(
            ILogger<ComicVinePersonProvider> logger,
            IHttpClientFactory httpClientFactory,
            IComicVineMetadataCacheManager comicVineMetadataCacheManager,
            IComicVineApiKeyProvider apiKeyProvider)
            : base(logger, comicVineMetadataCacheManager, httpClientFactory, apiKeyProvider)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _apiKeyProvider = apiKeyProvider;
        }

        /// <inheritdoc/>
        public string Name => ComicVineConstants.ProviderName;

        /// <inheritdoc/>
        public Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
