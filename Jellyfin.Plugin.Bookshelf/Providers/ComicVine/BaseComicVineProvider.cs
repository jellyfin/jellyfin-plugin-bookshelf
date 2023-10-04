using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    /// <summary>
    /// Base class for the Comic Vine providers.
    /// </summary>
    public abstract class BaseComicVineProvider
    {
        private readonly ILogger<BaseComicVineProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IComicVineMetadataCacheManager _comicVineMetadataCacheManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseComicVineProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{BaseComicVineProvider}"/> interface.</param>
        /// <param name="comicVineMetadataCacheManager">Instance of the <see cref="IComicVineMetadataCacheManager"/> interface.</param>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        protected BaseComicVineProvider(ILogger<BaseComicVineProvider> logger, IComicVineMetadataCacheManager comicVineMetadataCacheManager, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _comicVineMetadataCacheManager = comicVineMetadataCacheManager;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Gets the json options for deserializing the Comic Vine API responses.
        /// </summary>
        protected JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions(JsonDefaults.Options)
        {
            PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy()
        };

        /// <summary>
        /// Get the Comic Vine API key from the configuration.
        /// </summary>
        /// <returns>The API key or null.</returns>
        protected string? GetApiKey()
        {
            var apiKey = Plugin.Instance?.Configuration.ComicVineApiKey;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Comic Vine API key is not set.");
                return null;
            }

            return apiKey;
        }

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

            var apiKey = GetApiKey();

            if (apiKey == null)
            {
                return null;
            }

            var url = string.Format(CultureInfo.InvariantCulture, ComicVineApiUrls.IssueDetailUrl, apiKey, issueApiId);

            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);

            using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponse<IssueDetails>>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);

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
        protected IEnumerable<T> GetFromApiResponse<T>(ApiResponse<T> response)
        {
            if (response.IsError)
            {
                _logger.LogError("Comic Vine API response received with error code {ErrorCode} : {ErrorMessage}", response.StatusCode, response.Error);
                return Enumerable.Empty<T>();
            }

            return response.Results;
        }

        /// <summary>
        /// Gets the two part API id from the provider id ({slug}/{fixed-value}-{id}).
        /// </summary>
        /// <param name="providerId">Provider id.</param>
        /// <returns>The API id.</returns>
        protected static string GetIssueApiIdFromProviderId(string providerId)
        {
            return providerId.Split('/').Last();
        }
    }
}
