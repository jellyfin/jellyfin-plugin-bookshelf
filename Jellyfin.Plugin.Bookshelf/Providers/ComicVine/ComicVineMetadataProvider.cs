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
    /// Comic Vine metadata provider.
    /// </summary>
    public class ComicVineMetadataProvider : BaseComicVineProvider, IRemoteMetadataProvider<Book, BookInfo>
    {
        private readonly ILogger<ComicVineMetadataProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IComicVineApiKeyProvider _apiKeyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComicVineMetadataProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{ComicVineMetadataProvider}"/> interface.</param>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="comicVineMetadataCacheManager">Instance of the <see cref="IComicVineMetadataCacheManager"/> interface.</param>
        /// <param name="apiKeyProvider">Instance of the <see cref="IComicVineApiKeyProvider"/> interface.</param>
        public ComicVineMetadataProvider(ILogger<ComicVineMetadataProvider> logger, IHttpClientFactory httpClientFactory, IComicVineMetadataCacheManager comicVineMetadataCacheManager, IComicVineApiKeyProvider apiKeyProvider)
            : base(logger, comicVineMetadataCacheManager, httpClientFactory, apiKeyProvider)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _apiKeyProvider = apiKeyProvider;
        }

        /// <inheritdoc/>
        public string Name => ComicVineConstants.ProviderName;

        /// <inheritdoc/>
        public async Task<MetadataResult<Book>> GetMetadata(BookInfo info, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadataResult = new MetadataResult<Book>()
            {
                QueriedById = true
            };

            var issueProviderId = info.GetProviderId(ComicVineConstants.ProviderId);

            if (string.IsNullOrWhiteSpace(issueProviderId))
            {
                issueProviderId = await FetchIssueId(info, cancellationToken).ConfigureAwait(false);
                metadataResult.QueriedById = false;
            }

            if (string.IsNullOrWhiteSpace(issueProviderId))
            {
                return metadataResult;
            }

            var issueDetails = await GetOrAddItemDetailsFromCache<IssueDetails>(issueProviderId, cancellationToken).ConfigureAwait(false);

            if (issueDetails != null)
            {
                metadataResult.Item = new Book();
                metadataResult.Item.SetProviderId(ComicVineConstants.ProviderId, issueProviderId);
                metadataResult.HasMetadata = true;

                VolumeDetails? volumeDetails = null;

                if (!string.IsNullOrWhiteSpace(issueDetails.Volume?.SiteDetailUrl))
                {
                    volumeDetails = await GetOrAddItemDetailsFromCache<VolumeDetails>(GetProviderIdFromSiteDetailUrl(issueDetails.Volume.SiteDetailUrl), cancellationToken).ConfigureAwait(false);
                }

                ProcessIssueData(metadataResult.Item, issueDetails, volumeDetails, cancellationToken);
                ProcessIssueMetadata(metadataResult, issueDetails, cancellationToken);
            }

            return metadataResult;
        }

        /// <summary>
        /// Process the issue data.
        /// </summary>
        /// <param name="item">The Book item.</param>
        /// <param name="issue">The issue details.</param>
        /// <param name="volume">The volume details.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void ProcessIssueData(Book item, IssueDetails issue, VolumeDetails? volume, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            item.Name = !string.IsNullOrWhiteSpace(issue.Name) ? issue.Name : $"#{issue.IssueNumber.PadLeft(3, '0')}";

            string sortIssueName = issue.IssueNumber.PadLeft(3, '0');

            if (!string.IsNullOrWhiteSpace(issue.Volume?.Name))
            {
                sortIssueName += " - " + issue.Volume?.Name;
            }

            if (!string.IsNullOrWhiteSpace(issue.Name))
            {
                sortIssueName += ", " + issue.Name;
            }

            item.ForcedSortName = sortIssueName;

            item.SeriesName = issue.Volume?.Name;
            item.Overview = WebUtility.HtmlDecode(issue.Description);
            item.ProductionYear = GetYearFromCoverDate(issue.CoverDate);

            if (!string.IsNullOrWhiteSpace(volume?.Publisher?.Name))
            {
                item.AddStudio(volume.Publisher.Name);
            }
        }

        /// <summary>
        /// Process the issue metadata.
        /// </summary>
        /// <param name="item">The metadata result.</param>
        /// <param name="issue">The issue details.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void ProcessIssueMetadata(MetadataResult<Book> item, IssueDetails issue, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var person in issue.PersonCredits)
            {
                var personInfo = new PersonInfo
                {
                    Name = person.Name,
                    Type = person.Roles.Any() ? GetPersonKindFromRole(person.Roles.First()) : "Unknown"
                };

                personInfo.SetProviderId(ComicVineConstants.ProviderId, GetProviderIdFromSiteDetailUrl(person.SiteDetailUrl));

                item.AddPerson(personInfo);
            }
        }

        private string GetPersonKindFromRole(PersonCreditRole role)
        {
            return role switch
            {
                PersonCreditRole.Artist => "Artist",
                PersonCreditRole.Colorist => "Colorist",
                PersonCreditRole.Cover => "CoverArtist",
                PersonCreditRole.Editor => "Editor",
                PersonCreditRole.Inker => "Inker",
                PersonCreditRole.Letterer => "Letterer",
                PersonCreditRole.Penciler => "Penciller",
                PersonCreditRole.Translator => "Translator",
                PersonCreditRole.Writer => "Writer",
                PersonCreditRole.Assistant
                    or PersonCreditRole.Designer
                    or PersonCreditRole.Journalist
                    or PersonCreditRole.Production
                    or PersonCreditRole.Other => "Unknown",
                _ => throw new ArgumentException($"Unknown role: {role}"),
            };
        }

        /// <summary>
        /// Try to find the issue id from the item info.
        /// </summary>
        /// <param name="item">The BookInfo item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The issue id if found.</returns>
        private async Task<string?> FetchIssueId(BookInfo item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parsedItem = BookFileNameParser.GetBookMetadata(item);

            var searchResults = await GetSearchResultsInternal(parsedItem, cancellationToken)
                .ConfigureAwait(false);

            if (!searchResults.Any())
            {
                return null;
            }

            var comparableName = BookFileNameParser.GetComparableString(parsedItem.Name, false);
            var comparableSeriesName = BookFileNameParser.GetComparableString(parsedItem.SeriesName, false);

            foreach (var result in searchResults)
            {
                if (!int.TryParse(result.IssueNumber, out var issueNumber))
                {
                    continue;
                }

                // Match series name and issue number, and optionally the name

                var comparableVolumeName = BookFileNameParser.GetComparableString(result.Volume?.Name ?? string.Empty, false);
                if (issueNumber != parsedItem.IndexNumber || !comparableSeriesName.Equals(comparableVolumeName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(comparableName) && !string.IsNullOrWhiteSpace(result.Name))
                {
                    var comparableIssueName = BookFileNameParser.GetComparableString(result.Name, false);
                    if (!comparableName.Equals(comparableIssueName, StringComparison.Ordinal))
                    {
                        continue;
                    }
                }

                if (parsedItem.Year.HasValue)
                {
                    var resultYear = GetYearFromCoverDate(result.CoverDate);

                    if (Math.Abs(resultYear - parsedItem.Year ?? 0) > 1)
                    {
                        continue;
                    }
                }

                return GetProviderIdFromSiteDetailUrl(result.SiteDetailUrl);
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            return await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BookInfo searchInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Func<IssueSearch, RemoteSearchResult> getSearchResultFromIssue = (IssueSearch issue) =>
            {
                var remoteSearchResult = new RemoteSearchResult();

                remoteSearchResult.SetProviderId(ComicVineConstants.ProviderId, GetProviderIdFromSiteDetailUrl(issue.SiteDetailUrl));
                remoteSearchResult.SearchProviderName = ComicVineConstants.ProviderName;
                remoteSearchResult.Name = string.IsNullOrWhiteSpace(issue.Name) ? $"#{issue.IssueNumber.PadLeft(3, '0')}" : issue.Name;
                remoteSearchResult.Overview = string.IsNullOrWhiteSpace(issue.Description) ? string.Empty : WebUtility.HtmlDecode(issue.Description);
                remoteSearchResult.ProductionYear = GetYearFromCoverDate(issue.CoverDate);

                if (!string.IsNullOrWhiteSpace(issue.Image?.ThumbUrl))
                {
                    remoteSearchResult.ImageUrl = issue.Image.ThumbUrl;
                }

                return remoteSearchResult;
            };

            var issueProviderId = searchInfo.GetProviderId(ComicVineConstants.ProviderId);

            if (!string.IsNullOrWhiteSpace(issueProviderId))
            {
                var issueDetails = await GetOrAddItemDetailsFromCache<IssueDetails>(issueProviderId, cancellationToken).ConfigureAwait(false);

                if (issueDetails == null)
                {
                    return Enumerable.Empty<RemoteSearchResult>();
                }

                return new[] { getSearchResultFromIssue(issueDetails) };
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
                    list.Add(getSearchResultFromIssue(result));
                }

                return list;
            }
        }

        /// <summary>
        /// Get the year from the cover date.
        /// </summary>
        /// <param name="coverDate">The date, in the format "yyyy-MM-dd".</param>
        /// <returns>The year.</returns>
        private int? GetYearFromCoverDate(string coverDate)
        {
            if (DateTimeOffset.TryParse(coverDate, out var result))
            {
                return result.Year;
            }

            return null;
        }

        private async Task<IEnumerable<IssueSearch>> GetSearchResultsInternal(BookInfo item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var apiKey = _apiKeyProvider.GetApiKey();

            if (apiKey == null)
            {
                return Enumerable.Empty<IssueSearch>();
            }

            var searchString = GetSearchString(item);
            var url = string.Format(CultureInfo.InvariantCulture, ComicVineApiUrls.IssueSearchUrl, apiKey, WebUtility.UrlEncode(searchString));

            var response = await _httpClientFactory
                .CreateClient(NamedClient.Default)
                .GetAsync(url, cancellationToken)
                .ConfigureAwait(false);

            var apiResponse = await response.Content.ReadFromJsonAsync<SearchApiResponse<IssueSearch>>(JsonOptions, cancellationToken).ConfigureAwait(false);

            if (apiResponse == null)
            {
                _logger.LogError("Failed to deserialize Comic Vine API response.");
                return Enumerable.Empty<IssueSearch>();
            }

            var results = GetFromApiResponse<IssueSearch>(apiResponse);

            return results;
        }

        /// <summary>
        /// Get the search string for the item.
        /// Will try to use the format "{SeriesName} {IndexNumber} {Name}".
        /// </summary>
        /// <param name="item">The BookInfo item.</param>
        /// <returns>The search string.</returns>
        internal string GetSearchString(BookInfo item)
        {
            string result = string.Empty;

            if (!string.IsNullOrWhiteSpace(item.SeriesName))
            {
                result += $" {item.SeriesName}";
            }

            if (item.IndexNumber.HasValue)
            {
                result += $" {item.IndexNumber.Value}";
            }

            if (!string.IsNullOrWhiteSpace(item.Name))
            {
                result += $" {item.Name}";
            }

            return result.Trim();
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
        private static string GetProviderIdFromSiteDetailUrl(string siteDetailUrl)
        {
            return siteDetailUrl.Replace(ComicVineApiUrls.BaseWebsiteUrl, string.Empty, StringComparison.OrdinalIgnoreCase).Trim('/');
        }
    }
}
