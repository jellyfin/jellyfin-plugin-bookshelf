using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using Jellyfin.Plugin.Bookshelf.Providers.ComicVine.Models;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    /// <summary>
    /// Base class for the Comic Vine providers.
    /// </summary>
    public abstract class BaseComicVineProvider
    {
        private const string IssueIdMatchGroup = "issueId";

        private readonly ILogger<BaseComicVineProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IComicVineMetadataCacheManager _comicVineMetadataCacheManager;
        private readonly IComicVineApiKeyProvider _apiKeyProvider;

        private static readonly Regex[] _issueIdMatches = new[]
        {
            // The slug needs to be stored in the provider id for the IExternalId implementation
            new Regex(@"^(?<slug>.+?)\/(?<issueId>\d+-\d+)$"),
            // Also support the issue id on its own for manual searches
            new Regex(@"^(?<issueId>\d+-\d+)$")
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseComicVineProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{BaseComicVineProvider}"/> interface.</param>
        /// <param name="comicVineMetadataCacheManager">Instance of the <see cref="IComicVineMetadataCacheManager"/> interface.</param>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="apiKeyProvider">Instance of the <see cref="IComicVineApiKeyProvider"/> interface.</param>
        protected BaseComicVineProvider(ILogger<BaseComicVineProvider> logger, IComicVineMetadataCacheManager comicVineMetadataCacheManager, IHttpClientFactory httpClientFactory, IComicVineApiKeyProvider apiKeyProvider)
        {
            _logger = logger;
            _comicVineMetadataCacheManager = comicVineMetadataCacheManager;
            _httpClientFactory = httpClientFactory;
            _apiKeyProvider = apiKeyProvider;
        }

        /// <summary>
        /// Gets the json options for deserializing the Comic Vine API responses.
        /// </summary>
        protected JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions(JsonDefaults.Options)
        {
            PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy()
        };

        /// <summary>
        /// Get the details of an issue from the cache.
        /// If it's not already cached, fetch it from the API and add it to the cache.
        /// </summary>
        /// <param name="issueProviderId">The provider id for the issue.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The issue details, or null if not found.</returns>
        protected async Task<IssueDetails?> GetOrAddIssueDetailsFromCache(string issueProviderId, CancellationToken cancellationToken)
        {
            try
            {
                var issueApiId = GetIssueApiIdFromProviderId(issueProviderId);

                if (string.IsNullOrWhiteSpace(issueApiId))
                {
                    _logger.LogInformation("Couldn't get issue API id from provider id {IssueProviderId}.", issueProviderId);
                    return null;
                }

                if (!_comicVineMetadataCacheManager.HasCache(issueApiId))
                {
                    var issueDetails = await FetchIssueDetails(issueApiId, cancellationToken).ConfigureAwait(false);

                    if (issueDetails == null)
                    {
                        _logger.LogInformation("Issue {IssueApiId} was not found.", issueApiId);
                        return null;
                    }

                    _logger.LogInformation("Adding issue {IssueApiId} to the cache.", issueApiId);

                    await _comicVineMetadataCacheManager.AddToCache(issueApiId, issueDetails, cancellationToken).ConfigureAwait(false);

                    return issueDetails;
                }
                else
                {
                    _logger.LogInformation("Found issue {IssueApiId} in cache.", issueApiId);

                    return await _comicVineMetadataCacheManager.GetFromCache(issueApiId, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (FileNotFoundException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }

            return null;
        }

        /// <summary>
        /// Get the details for a specific issue from its id.
        /// </summary>
        /// <param name="issueApiId">The id of the issue.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The issue if found.</returns>
        private async Task<IssueDetails?> FetchIssueDetails(string issueApiId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var apiKey = _apiKeyProvider.GetApiKey();

            if (apiKey == null)
            {
                return null;
            }

            var url = string.Format(CultureInfo.InvariantCulture, ComicVineApiUrls.IssueDetailUrl, apiKey, issueApiId);

            var response = await _httpClientFactory
                .CreateClient(NamedClient.Default)
                .GetAsync(url, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Got non successful response code from Comic Vine API: {StatusCode}.", response.StatusCode);
                return null;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ItemApiResponse<IssueDetails>>(JsonOptions, cancellationToken).ConfigureAwait(false);

            if (apiResponse == null)
            {
                _logger.LogError("Failed to deserialize Comic Vine API response.");
                return null;
            }

            var results = GetFromApiResponse<IssueDetails>(apiResponse);

            if (results.Count() != 1)
            {
                _logger.LogError("Unexpected number of results in Comic Vine API response.");
                return null;
            }

            return results.Single();
        }

        /// <summary>
        /// Get the results from the API response.
        /// </summary>
        /// <typeparam name="T">Type of the results.</typeparam>
        /// <param name="response">API response.</param>
        /// <returns>The results.</returns>
        protected IEnumerable<T> GetFromApiResponse<T>(BaseApiResponse<T> response)
        {
            if (response.IsError)
            {
                _logger.LogError("Comic Vine API response received with error code {ErrorCode} : {ErrorMessage}", response.StatusCode, response.Error);
                return Enumerable.Empty<T>();
            }

            if (response is SearchApiResponse<T> searchResponse)
            {
                return searchResponse.Results;
            }
            else if (response is ItemApiResponse<T> itemResponse)
            {
                return itemResponse.Results == null ? Enumerable.Empty<T>() : new[] { itemResponse.Results };
            }
            else
            {
                return Enumerable.Empty<T>();
            }
        }

        /// <summary>
        /// Gets the two part API id from the provider id ({slug}/{fixed-value}-{id}).
        /// </summary>
        /// <param name="providerId">Provider id.</param>
        /// <returns>The API id.</returns>
        protected string? GetIssueApiIdFromProviderId(string providerId)
        {
            foreach (var regex in _issueIdMatches)
            {
                var match = regex.Match(providerId);

                if (!match.Success)
                {
                    continue;
                }

                if (match.Groups.ContainsKey(IssueIdMatchGroup))
                {
                    var value = match.Groups[IssueIdMatchGroup];
                    if (value.Success)
                    {
                        return value.Value;
                    }
                }
            }

            return null;
        }
    }
}
