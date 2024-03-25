using System;
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
            new Regex(@"^(?<slug>.+?)\/(?<issueId>[0-9]+-[0-9]+)$"),
            // Also support the issue id on its own for manual searches
            new Regex(@"^(?<issueId>[0-9]+-[0-9]+)$")
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
        /// Get the details of a resource item from the cache.
        /// If it's not already cached, fetch it from the API and add it to the cache.
        /// </summary>
        /// <param name="providerId">The provider id for the resource.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <returns>The resource details, or null if not found.</returns>
        protected async Task<T?> GetOrAddItemDetailsFromCache<T>(string providerId, CancellationToken cancellationToken)
        {
            try
            {
                var itemApiId = GetApiIdFromProviderId(providerId);

                if (string.IsNullOrWhiteSpace(itemApiId))
                {
                    _logger.LogInformation("Couldn't get API id from provider id {ProviderId}.", providerId);
                    return default;
                }

                if (!_comicVineMetadataCacheManager.HasCache(itemApiId))
                {
                    var itemDetails = await FetchItemDetails<T>(itemApiId, cancellationToken).ConfigureAwait(false);

                    if (itemDetails == null)
                    {
                        _logger.LogInformation("Resource with id {ApiId} was not found.", itemApiId);
                        return default;
                    }

                    _logger.LogInformation("Adding resource with id {ApiId} to the cache.", itemApiId);

                    await _comicVineMetadataCacheManager.AddToCache<T>(itemApiId, itemDetails, cancellationToken).ConfigureAwait(false);

                    return itemDetails;
                }
                else
                {
                    _logger.LogInformation("Found resource with id {ApiId} in the cache.", itemApiId);

                    return await _comicVineMetadataCacheManager.GetFromCache<T>(itemApiId, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (FileNotFoundException fileEx)
            {
                _logger.LogWarning("Cannot find cache file {FileName}.", fileEx.FileName);
            }
            catch (DirectoryNotFoundException directoryEx)
            {
                _logger.LogWarning("Cannot find cache directory: {ExceptionMessage}.", directoryEx.Message);
            }

            return default;
        }

        /// <summary>
        /// Get the details for a specific resource from its id.
        /// </summary>
        /// <param name="apiId">The id of the resource.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The resource details if found.</returns>
        private async Task<T?> FetchItemDetails<T>(string apiId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var apiKey = _apiKeyProvider.GetApiKey();

            if (apiKey == null)
            {
                return default;
            }

            string resourceDetailsUrl = typeof(T) switch
            {
                Type issue when issue == typeof(IssueDetails) => ComicVineApiUrls.IssueDetailUrl,
                Type volume when volume == typeof(VolumeDetails) => ComicVineApiUrls.VolumeDetailUrl,
                _ => throw new InvalidOperationException($"Unexpected resource type {typeof(T)}.")
            };

            var url = string.Format(CultureInfo.InvariantCulture, resourceDetailsUrl, apiKey, apiId);

            var response = await _httpClientFactory
                .CreateClient(NamedClient.Default)
                .GetAsync(url, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Got non successful response code from Comic Vine API: {StatusCode}.", response.StatusCode);
                return default;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ItemApiResponse<T>>(JsonOptions, cancellationToken).ConfigureAwait(false);

            if (apiResponse == null)
            {
                _logger.LogError("Failed to deserialize Comic Vine API response.");
                return default;
            }

            var results = GetFromApiResponse<T>(apiResponse);

            if (results.Count != 1)
            {
                _logger.LogError("Unexpected number of results in Comic Vine API response.");
                return default;
            }

            return results.Single();
        }

        /// <summary>
        /// Get the results from the API response.
        /// </summary>
        /// <typeparam name="T">Type of the results.</typeparam>
        /// <param name="response">API response.</param>
        /// <returns>The results.</returns>
        protected IReadOnlyList<T> GetFromApiResponse<T>(BaseApiResponse<T> response)
        {
            if (response.IsError)
            {
                _logger.LogError("Comic Vine API response received with error code {ErrorCode} : {ErrorMessage}", response.StatusCode, response.Error);
                return Array.Empty<T>();
            }

            if (response is SearchApiResponse<T> searchResponse)
            {
                return searchResponse.Results.ToList();
            }
            else if (response is ItemApiResponse<T> itemResponse)
            {
                return itemResponse.Results == null ? Array.Empty<T>() : [itemResponse.Results];
            }
            else
            {
                return Array.Empty<T>();
            }
        }

        /// <summary>
        /// Gets the two part API id from the provider id ({slug}/{fixed-value}-{id}).
        /// </summary>
        /// <param name="providerId">Provider id.</param>
        /// <returns>The API id.</returns>
        protected string? GetApiIdFromProviderId(string providerId)
        {
            foreach (var regex in _issueIdMatches)
            {
                var match = regex.Match(providerId);

                if (!match.Success)
                {
                    continue;
                }

                if (match.Groups.TryGetValue(IssueIdMatchGroup, out Group? issueIdGroup) && issueIdGroup.Success)
                {
                    return issueIdGroup.Value;
                }
            }

            return null;
        }
    }
}
