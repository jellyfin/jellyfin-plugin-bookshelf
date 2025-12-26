using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Bookshelf.Providers.ComicVine.Models;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine;

/// <summary>
/// Comic Vine person image provider.
/// </summary>
public class ComicVinePersonImageProvider : BaseComicVineProvider, IRemoteImageProvider
{
    private readonly ILogger<ComicVinePersonImageProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComicVinePersonImageProvider"/> class.
    /// </summary>
    /// <param name="comicVineMetadataCacheManager">Instance of the <see cref="IComicVineMetadataCacheManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{ComicVinePersonImageProvider}"/> interface.</param>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="apiKeyProvider">Instance of the <see cref="IComicVineApiKeyProvider"/> interface.</param>
    public ComicVinePersonImageProvider(
        IComicVineMetadataCacheManager comicVineMetadataCacheManager,
        ILogger<ComicVinePersonImageProvider> logger,
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
        return item is Person;
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

        var personProviderId = item.GetProviderId(ComicVineConstants.ProviderId);

        if (string.IsNullOrWhiteSpace(personProviderId))
        {
            return Enumerable.Empty<RemoteImageInfo>();
        }

        var personDetails = await GetOrAddItemDetailsFromCache<PersonDetails>(personProviderId, cancellationToken).ConfigureAwait(false);

        if (personDetails == null)
        {
            return Enumerable.Empty<RemoteImageInfo>();
        }

        var images = ProcessImages(personDetails.Image)
            .Select(url => new RemoteImageInfo
            {
                Url = url,
                ProviderName = ComicVineConstants.ProviderName
            });

        return images;
    }

    /// <inheritdoc/>
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
    }
}
