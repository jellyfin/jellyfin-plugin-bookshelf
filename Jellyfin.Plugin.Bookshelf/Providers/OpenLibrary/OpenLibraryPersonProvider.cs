using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.OpenLibrary
{
    /// <summary>
    /// OpenLibrary person metadata provider for authors.
    /// </summary>
    public class OpenLibraryPersonProvider : IRemoteMetadataProvider<Person, PersonLookupInfo>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OpenLibraryPersonProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenLibraryPersonProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{OpenLibraryPersonProvider}"/> interface.</param>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        public OpenLibraryPersonProvider(
            ILogger<OpenLibraryPersonProvider> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <inheritdoc />
        public string Name => "OpenLibrary";

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(searchInfo.Name))
            {
                return Enumerable.Empty<RemoteSearchResult>();
            }

            try
            {
                var searchResults = await SearchOpenLibraryAuthors(searchInfo.Name, cancellationToken).ConfigureAwait(false);
                var remoteResults = new List<RemoteSearchResult>();

                foreach (var result in searchResults.Take(5)) // Limit to top 5 results
                {
                    var remoteResult = new RemoteSearchResult
                    {
                        Name = result.Name,
                        SearchProviderName = Name,
                        Overview = result.Bio
                    };

                    remoteResult.SetProviderId("OpenLibrary", result.Key);

                    if (result.BirthDate != null)
                    {
                        if (DateTime.TryParse(result.BirthDate, out var birthDate))
                        {
                            remoteResult.PremiereDate = birthDate;
                        }
                    }

                    remoteResults.Add(remoteResult);
                }

                return remoteResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching OpenLibrary for person: {Name}", searchInfo.Name);
                return Enumerable.Empty<RemoteSearchResult>();
            }
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Person>();

            if (string.IsNullOrWhiteSpace(info.Name))
            {
                return result;
            }

            try
            {
                // Check if we have an OpenLibrary ID first
                var openLibraryId = info.GetProviderId("OpenLibrary");
                OpenLibraryAuthorResult? authorResult = null;

                if (!string.IsNullOrEmpty(openLibraryId))
                {
                    // Direct lookup by ID
                    authorResult = await GetAuthorByKey(openLibraryId, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Search for the author
                    var searchResults = await SearchOpenLibraryAuthors(info.Name, cancellationToken).ConfigureAwait(false);
                    authorResult = searchResults.FirstOrDefault(a =>
                        a.Name.Equals(info.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (authorResult != null)
                {
                    var person = new Person
                    {
                        Name = authorResult.Name,
                        Overview = authorResult.Bio
                    };

                    if (authorResult.BirthDate != null && DateTime.TryParse(authorResult.BirthDate, out var birthDate))
                    {
                        person.PremiereDate = birthDate;
                    }

                    if (authorResult.DeathDate != null && DateTime.TryParse(authorResult.DeathDate, out var deathDate))
                    {
                        person.EndDate = deathDate;
                    }

                    person.SetProviderId("OpenLibrary", authorResult.Key);

                    result.Item = person;
                    result.HasMetadata = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OpenLibrary person metadata for {Name}", info.Name);
            }

            return result;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("OpenLibrary person provider does not support image retrieval");
        }

        private async Task<List<OpenLibraryAuthorResult>> SearchOpenLibraryAuthors(string name, CancellationToken cancellationToken)
        {
            var searchUrl = $"https://openlibrary.org/search/authors.json?q={HttpUtility.UrlEncode(name)}&limit=10";

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            try
            {
                var response = await httpClient.GetAsync(searchUrl, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenLibrary author search failed with status: {StatusCode} for: {Name}", response.StatusCode, name);
                    return new List<OpenLibraryAuthorResult>();
                }

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return ParseAuthorSearchResults(jsonContent);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning("OpenLibrary author search timed out for: {Name}", name);
                return new List<OpenLibraryAuthorResult>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching OpenLibrary authors for: {Name}", name);
                return new List<OpenLibraryAuthorResult>();
            }
        }

        private async Task<OpenLibraryAuthorResult?> GetAuthorByKey(string authorKey, CancellationToken cancellationToken)
        {
            var authorUrl = $"https://openlibrary.org/authors/{authorKey}.json";

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            try
            {
                var response = await httpClient.GetAsync(authorUrl, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenLibrary author detail failed with status: {StatusCode} for: {AuthorKey}", response.StatusCode, authorKey);
                    return null;
                }

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return ParseAuthorDetails(jsonContent, authorKey);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning("OpenLibrary author detail timed out for: {AuthorKey}", authorKey);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OpenLibrary author details for: {AuthorKey}", authorKey);
                return null;
            }
        }

        private List<OpenLibraryAuthorResult> ParseAuthorSearchResults(string jsonContent)
        {
            var results = new List<OpenLibraryAuthorResult>();

            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                if (root.TryGetProperty("docs", out var docsElement))
                {
                    foreach (var doc in docsElement.EnumerateArray())
                    {
                        var authorResult = new OpenLibraryAuthorResult();

                        if (doc.TryGetProperty("key", out var keyElement))
                        {
                            authorResult.Key = keyElement.GetString()?.Replace("/authors/", string.Empty, StringComparison.Ordinal) ?? string.Empty;
                        }

                        if (doc.TryGetProperty("name", out var nameElement))
                        {
                            authorResult.Name = nameElement.GetString() ?? string.Empty;
                        }

                        if (doc.TryGetProperty("birth_date", out var birthElement))
                        {
                            authorResult.BirthDate = birthElement.GetString();
                        }

                        if (doc.TryGetProperty("death_date", out var deathElement))
                        {
                            authorResult.DeathDate = deathElement.GetString();
                        }

                        if (!string.IsNullOrEmpty(authorResult.Key) && !string.IsNullOrEmpty(authorResult.Name))
                        {
                            results.Add(authorResult);
                        }
                    }
                }

                // Fetch detailed information including bios for each author
                foreach (var author in results)
                {
                    try
                    {
                        var detailedAuthor = GetAuthorByKey(author.Key, CancellationToken.None).Result;
                        if (detailedAuthor?.Bio != null)
                        {
                            author.Bio = detailedAuthor.Bio;
                        }
                    }
                    catch
                    {
                        // Continue without bio if detailed fetch fails
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing OpenLibrary author search results");
            }

            return results;
        }

        private OpenLibraryAuthorResult? ParseAuthorDetails(string jsonContent, string authorKey)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                var result = new OpenLibraryAuthorResult
                {
                    Key = authorKey
                };

                if (root.TryGetProperty("name", out var nameElement))
                {
                    result.Name = nameElement.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("bio", out var bioElement))
                {
                    result.Bio = ExtractStringOrTextValue(bioElement);
                }

                if (root.TryGetProperty("birth_date", out var birthElement))
                {
                    result.BirthDate = birthElement.GetString();
                }

                if (root.TryGetProperty("death_date", out var deathElement))
                {
                    result.DeathDate = deathElement.GetString();
                }

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing OpenLibrary author details for: {AuthorKey}", authorKey);
                return null;
            }
        }

        private static string? ExtractStringOrTextValue(JsonElement element)
        {
            // OpenLibrary sometimes stores descriptions as objects with "value" property
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("value", out var valueElement))
                {
                    return valueElement.GetString();
                }
            }
            else if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }

            return null;
        }

        private class OpenLibraryAuthorResult
        {
            public string Key { get; set; } = string.Empty;

            public string Name { get; set; } = string.Empty;

            public string? Bio { get; set; }

            public string? BirthDate { get; set; }

            public string? DeathDate { get; set; }
        }
    }
}
