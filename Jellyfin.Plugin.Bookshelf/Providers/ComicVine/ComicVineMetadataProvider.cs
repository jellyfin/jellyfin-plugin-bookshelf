using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Bookshelf.Common;
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
        private readonly IComicVineMetadataCacheManager _comicVineMetadataCacheManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComicVineMetadataProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{ComicVineMetadataProvider}"/> interface.</param>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="comicVineMetadataCacheManager">Instance of the <see cref="IComicVineMetadataCacheManager"/> interface.</param>
        public ComicVineMetadataProvider(ILogger<ComicVineMetadataProvider> logger, IHttpClientFactory httpClientFactory, IComicVineMetadataCacheManager comicVineMetadataCacheManager)
            : base(logger, comicVineMetadataCacheManager, httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _comicVineMetadataCacheManager = comicVineMetadataCacheManager;
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

            var issueWebsiteId = info.GetProviderId(ComicVineConstants.ProviderId);

            if (string.IsNullOrWhiteSpace(issueWebsiteId))
            {
                issueWebsiteId = await FetchIssueId(info, cancellationToken).ConfigureAwait(false);
                metadataResult.QueriedById = false;
            }

            if (string.IsNullOrWhiteSpace(issueWebsiteId))
            {
                return metadataResult;
            }

            var issueDetails = await GetOrAddIssueDetailsFromCache(issueWebsiteId, cancellationToken).ConfigureAwait(false);

            if (issueDetails != null)
            {
                metadataResult.Item = new Book();
                metadataResult.Item.SetProviderId(ComicVineConstants.ProviderId, issueWebsiteId);
                metadataResult.HasMetadata = true;

                ProcessIssueData(metadataResult.Item, issueDetails, cancellationToken);
                ProcessIssueMetadata(metadataResult, issueDetails, cancellationToken);
            }

            return metadataResult;
        }

        /// <summary>
        /// Process the issue data.
        /// </summary>
        /// <param name="item">The Book item.</param>
        /// <param name="issue">The issue details.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void ProcessIssueData(Book item, IssueDetails issue, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(issue.Name))
            {
                item.Name = issue.Name;
            }
            else
            {
                item.Name = $"#{issue.IssueNumber.PadLeft(3, '0')}";
            }

            string sortIssueName = issue.IssueNumber.PadLeft(3, '0');

            sortIssueName += " - " + issue.Volume?.Name;

            if (!string.IsNullOrEmpty(issue.Name))
            {
                sortIssueName += ", " + issue.Name;
            }

            item.ForcedSortName = sortIssueName;

            item.SeriesName = issue.Volume?.Name;
            item.Overview = WebUtility.HtmlDecode(issue.Description);
            item.ProductionYear = GetYearFromCoverDate(issue.CoverDate);
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
                item.AddPerson(new PersonInfo
                {
                    Name = person.Name,
                    Type = person.Role, // TODO: Separate by comma
                });
            }
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

            // var comparableName = GetComparableName(parsedItem.Name, parsedItem.SeriesName, parsedItem.IndexNumber);

            foreach (var result in searchResults)
            {
                if (!int.TryParse(result.IssueNumber, out var issueNumber))
                {
                    continue;
                }

                // TODO: Update name comparison
                /*
                if (!GetComparableName(result.Name, result.Volume?.Name, issueNumber).Equals(comparableName, StringComparison.Ordinal))
                {
                    continue;
                }
                */

                if (parsedItem.Year.HasValue)
                {
                    var resultYear = GetYearFromCoverDate(result.CoverDate);

                    if (Math.Abs(resultYear - parsedItem.Year ?? 0) > 1)
                    {
                        continue;
                    }
                }

                return result.ApiDetailUrl;
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

            IEnumerable<IssueSearch> searchResults = await GetSearchResultsInternal(searchInfo, cancellationToken).ConfigureAwait(false);
            if (!searchResults.Any())
            {
                return Enumerable.Empty<RemoteSearchResult>();
            }

            var list = new List<RemoteSearchResult>();

            foreach (var result in searchResults)
            {
                var remoteSearchResult = new RemoteSearchResult();

                remoteSearchResult.SetProviderId(ComicVineConstants.ProviderId, GetIssueProviderIdFromSiteDetailUrl(result.SiteDetailUrl));
                remoteSearchResult.SearchProviderName = ComicVineConstants.ProviderName;
                remoteSearchResult.Name = result.Name;
                remoteSearchResult.Overview = WebUtility.HtmlDecode(result.Description);
                remoteSearchResult.ProductionYear = GetYearFromCoverDate(result.CoverDate);

                if (!string.IsNullOrWhiteSpace(result.Image?.ThumbUrl))
                {
                    remoteSearchResult.ImageUrl = result.Image.ThumbUrl;
                }

                list.Add(remoteSearchResult);
            }

            return list;
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

            var apiKey = GetApiKey();

            if (apiKey == null)
            {
                return Enumerable.Empty<IssueSearch>();
            }

            var searchString = GetSearchString(item);
            var url = string.Format(CultureInfo.InvariantCulture, ComicVineApiUrls.IssueSearchUrl, apiKey, WebUtility.UrlEncode(searchString));

            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);

            using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            var apiResponse = await JsonSerializer.DeserializeAsync<ApiResponse<IssueSearch>>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);

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
                result += $"{result} {item.SeriesName}".Trim();
            }

            if (item.IndexNumber.HasValue)
            {
                result = $"{result} {item.IndexNumber.Value}".Trim();
            }

            if (!string.IsNullOrWhiteSpace(item.Name))
            {
                result = $"{result} {item.Name}".Trim();
            }

            return result;
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
        private static string GetIssueProviderIdFromSiteDetailUrl(string siteDetailUrl)
        {
            return siteDetailUrl.Replace(ComicVineApiUrls.BaseWebsiteUrl, string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }
}
