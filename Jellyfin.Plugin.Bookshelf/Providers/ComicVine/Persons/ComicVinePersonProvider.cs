using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Bookshelf.Common;
using Jellyfin.Plugin.Bookshelf.Providers.ComicVine.Models;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
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
        public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
        {
            var metadataResult = new MetadataResult<Person>()
            {
                QueriedById = true
            };

            var personProviderId = info.GetProviderId(ComicVineConstants.ProviderId);

            if (string.IsNullOrWhiteSpace(personProviderId))
            {
                personProviderId = await FetchPersonId(info, cancellationToken).ConfigureAwait(false);
                metadataResult.QueriedById = false;
            }

            if (string.IsNullOrWhiteSpace(personProviderId))
            {
                return metadataResult;
            }

            var personDetails = await GetOrAddItemDetailsFromCache<PersonDetails>(personProviderId, cancellationToken).ConfigureAwait(false);

            if (personDetails != null)
            {
                metadataResult.HasMetadata = true;

                var person = new Person();
                person.SetProviderId(ComicVineConstants.ProviderId, personProviderId);

                person.Name = personDetails.Name;
                person.HomePageUrl = personDetails.Website ?? string.Empty;
                person.Overview = personDetails.Description ?? personDetails.Deck ?? string.Empty;

                // Replace relarive urls with absolute urls
                person.Overview = person.Overview?.Replace("href=\"", $"href=\"{ComicVineApiUrls.BaseWebsiteUrl}", StringComparison.Ordinal);

                person.PremiereDate = personDetails.BirthDate;
                person.EndDate = personDetails.DeathDate;

                if (!string.IsNullOrWhiteSpace(personDetails.Hometown))
                {
                    person.ProductionLocations = new[] { personDetails.Hometown };
                }

                if (!string.IsNullOrWhiteSpace(personDetails.Aliases))
                {
                    var splittedAliases = personDetails.Aliases.Split('\n');
                    if (splittedAliases.Any())
                    {
                        person.OriginalTitle = splittedAliases.First();
                    }
                }

                metadataResult.Item = person;
            }

            return metadataResult;
        }

        private async Task<string?> FetchPersonId(PersonLookupInfo item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var searchResults = await GetSearchResultsInternal(item, cancellationToken)
                .ConfigureAwait(false);

            if (!searchResults.Any())
            {
                return null;
            }

            var comparableName = BookFileNameParser.GetComparableString(item.Name, false);

            foreach (var result in searchResults)
            {
                var comparablePersonName = BookFileNameParser.GetComparableString(result.Name, false);
                if (!comparableName.Equals(comparablePersonName, StringComparison.Ordinal))
                {
                    continue;
                }

                return GetProviderIdFromSiteDetailUrl(result.SiteDetailUrl);
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            Func<PersonDetails, RemoteSearchResult> getSearchResultFromPerson = (PersonDetails person) =>
            {
                var remoteSearchResult = new RemoteSearchResult();

                remoteSearchResult.SetProviderId(ComicVineConstants.ProviderId, GetProviderIdFromSiteDetailUrl(person.SiteDetailUrl));
                remoteSearchResult.SearchProviderName = ComicVineConstants.ProviderName;
                remoteSearchResult.Name = person.Name;
                remoteSearchResult.Overview = person.Deck ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(person.Image?.ThumbUrl))
                {
                    remoteSearchResult.ImageUrl = person.Image.ThumbUrl;
                }

                return remoteSearchResult;
            };

            var personProviderId = searchInfo.GetProviderId(ComicVineConstants.ProviderId);

            if (!string.IsNullOrWhiteSpace(personProviderId))
            {
                var personDetails = await GetOrAddItemDetailsFromCache<PersonDetails>(personProviderId, cancellationToken).ConfigureAwait(false);

                if (personDetails == null)
                {
                    return Enumerable.Empty<RemoteSearchResult>();
                }

                return new[] { getSearchResultFromPerson(personDetails) };
            }
            else
            {
                var searchResults = await GetSearchResultsInternal(searchInfo, cancellationToken).ConfigureAwait(false);
                if (!searchResults.Any())
                {
                    return Enumerable.Empty<RemoteSearchResult>();
                }

                var list = new List<RemoteSearchResult>();
                foreach (var result in searchResults)
                {
                    list.Add(getSearchResultFromPerson(result));
                }

                return list;
            }
        }

        private async Task<IEnumerable<PersonDetails>> GetSearchResultsInternal(PersonLookupInfo item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var apiKey = _apiKeyProvider.GetApiKey();

            if (apiKey == null)
            {
                return Enumerable.Empty<PersonDetails>();
            }

            var url = string.Format(CultureInfo.InvariantCulture, ComicVineApiUrls.PersonSearchUrl, apiKey, WebUtility.UrlEncode(item.Name));

            var response = await _httpClientFactory
                .CreateClient(NamedClient.Default)
                .GetAsync(url, cancellationToken)
                .ConfigureAwait(false);

            var apiResponse = await response.Content.ReadFromJsonAsync<SearchApiResponse<PersonDetails>>(JsonOptions, cancellationToken).ConfigureAwait(false);

            if (apiResponse == null)
            {
                _logger.LogError("Failed to deserialize Comic Vine API response.");
                return Enumerable.Empty<PersonDetails>();
            }

            var results = GetFromApiResponse<PersonDetails>(apiResponse);

            return results;
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
