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
                Type person when person == typeof(PersonDetails) => ComicVineApiUrls.PersonDetailUrl,
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

        /// <summary>
        /// Gets images URLs from a list of images.
        /// </summary>
        /// <param name="imageList">The list of images.</param>
        /// <returns>The list of images URLs.</returns>
        protected IEnumerable<string> ProcessImages(ImageList? imageList)
        {
            if (imageList == null)
            {
                return Enumerable.Empty<string>();
            }

            var images = new List<string>();

            if (!string.IsNullOrWhiteSpace(imageList.SuperUrl))
            {
                images.Add(imageList.SuperUrl);
            }
            else if (!string.IsNullOrWhiteSpace(imageList.OriginalUrl))
            {
                images.Add(imageList.OriginalUrl);
            }
            else if (!string.IsNullOrWhiteSpace(imageList.MediumUrl))
            {
                images.Add(imageList.MediumUrl);
            }
            else if (!string.IsNullOrWhiteSpace(imageList.SmallUrl))
            {
                images.Add(imageList.SmallUrl);
            }
            else if (!string.IsNullOrWhiteSpace(imageList.ThumbUrl))
            {
                images.Add(imageList.ThumbUrl);
            }

            return images;
        }

        /// <summary>
        /// Gets the issue id from the site detail URL.
        /// <para>
        /// Issues have a unique Id, but also a different one used for the API.
        /// The URL to the issue detail page also includes a slug before the id.
        /// </para>
        /// <listheader>For example:</listheader>
        /// <list type="bullet">
        ///     <item>
        ///         <term>id</term>
        ///         <description>441467</description>
        ///     </item>
        ///     <item>
        ///         <term>api_detail_url</term>
        ///         <description>https://comicvine.gamespot.com/api/issue/4000-441467</description>
        ///     </item>
        ///     <item>
        ///         <term>site_detail_url</term>
        ///         <description>https://comicvine.gamespot.com/attack-on-titan-10-fortress-of-blood/4000-441467</description>
        ///     </item>
        /// </list>
        /// <para>
        /// We need to keep the last two parts of the site detail URL (the slug and the id) as the provider id for the IExternalId implementation to work.
        /// </para>
        /// </summary>
        /// <param name="siteDetailUrl">The site detail URL.</param>
        /// <returns>The slug and id.</returns>
        protected static string GetProviderIdFromSiteDetailUrl(string siteDetailUrl)
        {
            return siteDetailUrl.Replace(ComicVineApiUrls.BaseWebsiteUrl, string.Empty, StringComparison.OrdinalIgnoreCase).Trim('/');
        }
    }
}
