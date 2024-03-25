using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Bookshelf.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    /// <summary>
    /// Google books provider.
    /// </summary>
    public class GoogleBooksProvider : BaseGoogleBooksProvider, IRemoteMetadataProvider<Book, BookInfo>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GoogleBooksProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleBooksProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{GoogleBooksProvider}"/> interface.</param>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        public GoogleBooksProvider(
            ILogger<GoogleBooksProvider> logger,
            IHttpClientFactory httpClientFactory)
            : base(logger, httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => GoogleBooksConstants.ProviderName;

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BookInfo searchInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Func<BookResult, RemoteSearchResult> getSearchResultFromBook = (BookResult info) =>
            {
                var remoteSearchResult = new RemoteSearchResult();

                remoteSearchResult.SetProviderId(GoogleBooksConstants.ProviderId, info.Id);
                remoteSearchResult.SearchProviderName = GoogleBooksConstants.ProviderName;
                remoteSearchResult.Name = info.VolumeInfo?.Title;
                remoteSearchResult.Overview = WebUtility.HtmlDecode(info.VolumeInfo?.Description);
                remoteSearchResult.ProductionYear = GetYearFromPublishedDate(info.VolumeInfo?.PublishedDate);

                if (info.VolumeInfo?.ImageLinks?.Thumbnail != null)
                {
                    remoteSearchResult.ImageUrl = info.VolumeInfo.ImageLinks.Thumbnail;
                }

                return remoteSearchResult;
            };

            var googleBookId = searchInfo.GetProviderId(GoogleBooksConstants.ProviderId);

            if (!string.IsNullOrWhiteSpace(googleBookId))
            {
                var bookData = await FetchBookData(googleBookId, cancellationToken).ConfigureAwait(false);

                if (bookData == null || bookData.VolumeInfo == null)
                {
                    return Enumerable.Empty<RemoteSearchResult>();
                }

                return new[] { getSearchResultFromBook(bookData) };
            }
            else
            {
                var searchResults = await GetSearchResultsInternal(searchInfo, cancellationToken).ConfigureAwait(false);
                if (searchResults is null)
                {
                    return Enumerable.Empty<RemoteSearchResult>();
                }

                var list = new List<RemoteSearchResult>();
                foreach (var result in searchResults.Items)
                {
                    if (result.VolumeInfo is null)
                    {
                        continue;
                    }

                    list.Add(getSearchResultFromBook(result));
                }

                return list;
            }
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Book>> GetMetadata(BookInfo info, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadataResult = new MetadataResult<Book>()
            {
                QueriedById = true
            };

            var googleBookId = info.GetProviderId(GoogleBooksConstants.ProviderId);

            if (string.IsNullOrWhiteSpace(googleBookId))
            {
                googleBookId = await FetchBookId(info, cancellationToken).ConfigureAwait(false);
                metadataResult.QueriedById = false;
            }

            if (string.IsNullOrWhiteSpace(googleBookId))
            {
                return metadataResult;
            }

            var bookResult = await FetchBookData(googleBookId, cancellationToken).ConfigureAwait(false);

            if (bookResult == null)
            {
                return metadataResult;
            }

            var bookMetadataResult = ProcessBookData(bookResult, cancellationToken);
            if (bookMetadataResult == null)
            {
                return metadataResult;
            }

            ProcessBookMetadata(metadataResult, bookResult);

            metadataResult.Item = bookMetadataResult;
            metadataResult.HasMetadata = true;

            return metadataResult;
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            return await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        }

        private async Task<SearchResult?> GetSearchResultsInternal(BookInfo item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var searchString = GetSearchString(item);
            var url = string.Format(CultureInfo.InvariantCulture, GoogleApiUrls.SearchUrl, WebUtility.UrlEncode(searchString), 0, 20);

            return await GetResultFromAPI<SearchResult>(url, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string?> FetchBookId(BookInfo item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // pattern match the filename
            // year can be included for better results
            var parsedItem = BookFileNameParser.GetBookMetadata(item);

            var searchResults = await GetSearchResultsInternal(parsedItem, cancellationToken)
                .ConfigureAwait(false);
            if (searchResults?.Items == null)
            {
                return null;
            }

            var comparableName = GetComparableName(parsedItem.Name, parsedItem.SeriesName, parsedItem.IndexNumber);
            foreach (var i in searchResults.Items)
            {
                if (i.VolumeInfo is null)
                {
                    continue;
                }

                // no match so move on to the next item
                if (!GetComparableName(i.VolumeInfo.Title).Equals(comparableName, StringComparison.Ordinal))
                {
                    continue;
                }

                // adjust for google yyyy-mm-dd format
                var resultYear = GetYearFromPublishedDate(i.VolumeInfo.PublishedDate);
                if (resultYear == null)
                {
                    continue;
                }

                // allow a one year variance
                if (Math.Abs(resultYear - parsedItem.Year ?? 0) > 1)
                {
                    continue;
                }

                return i.Id;
            }

            return null;
        }

        private int? GetYearFromPublishedDate(string? publishedDate)
        {
            var resultYear = publishedDate?.Length > 4 ? publishedDate[..4] : publishedDate;

            if (!int.TryParse(resultYear, out var bookReleaseYear))
            {
                return null;
            }

            return bookReleaseYear;
        }

        private Book? ProcessBookData(BookResult bookResult, CancellationToken cancellationToken)
        {
            if (bookResult.VolumeInfo is null)
            {
                return null;
            }

            var book = new Book();
            cancellationToken.ThrowIfCancellationRequested();

            book.Name = bookResult.VolumeInfo.Title;
            book.Overview = WebUtility.HtmlDecode(bookResult.VolumeInfo.Description);
            book.ProductionYear = GetYearFromPublishedDate(bookResult.VolumeInfo.PublishedDate);

            if (!string.IsNullOrWhiteSpace(bookResult.VolumeInfo.Publisher))
            {
                book.AddStudio(bookResult.VolumeInfo.Publisher);
            }

            HashSet<string> categories = new HashSet<string>();

            // Categories are from the BISAC list (https://www.bisg.org/complete-bisac-subject-headings-list)
            // Keep the first one (most general) as genre, and add the rest as tags (while dropping the "General" tag)
            foreach (var category in bookResult.VolumeInfo.Categories)
            {
                foreach (var subCategory in category.Split('/', StringSplitOptions.TrimEntries))
                {
                    if (subCategory == "General")
                    {
                        continue;
                    }

                    categories.Add(subCategory);
                }
            }

            if (categories.Count > 0)
            {
                book.AddGenre(categories.First());
                foreach (var category in categories.Skip(1))
                {
                    book.AddTag(category);
                }
            }

            if (bookResult.VolumeInfo.AverageRating.HasValue)
            {
                // google rates out of five so convert to ten
                book.CommunityRating = bookResult.VolumeInfo.AverageRating.Value * 2;
            }

            if (!string.IsNullOrWhiteSpace(bookResult.Id))
            {
                book.SetProviderId(GoogleBooksConstants.ProviderId, bookResult.Id);
            }

            return book;
        }

        private void ProcessBookMetadata(MetadataResult<Book> metadataResult, BookResult bookResult)
        {
            if (bookResult.VolumeInfo == null)
            {
                return;
            }

            foreach (var author in bookResult.VolumeInfo.Authors)
            {
                metadataResult.AddPerson(new PersonInfo
                {
                    Name = author,
                    Type = PersonKind.Author,
                });
            }

            if (!string.IsNullOrWhiteSpace(bookResult.VolumeInfo.Language))
            {
                metadataResult.ResultLanguage = bookResult.VolumeInfo.Language;
            }
        }

        /// <summary>
        /// Get the search string for the item.
        /// If the item is part of a series, use the series name and the issue name or index.
        /// Otherwise, use the book name and year.
        /// </summary>
        /// <param name="item">BookInfo item.</param>
        /// <returns>The search query.</returns>
        internal string GetSearchString(BookInfo item)
        {
            string result = string.Empty;

            if (!string.IsNullOrWhiteSpace(item.SeriesName))
            {
                result = item.SeriesName;

                if (!string.IsNullOrWhiteSpace(item.Name))
                {
                    result = $"{result} {item.Name}";
                }
                else if (item.IndexNumber.HasValue)
                {
                    result = $"{result} {item.IndexNumber.Value}";
                }
            }
            else if (!string.IsNullOrWhiteSpace(item.Name))
            {
                result = item.Name;

                if (item.Year.HasValue)
                {
                    result = $"{result} {item.Year.Value}";
                }
            }

            return result;
        }

        /// <summary>
        /// Format information about a book to a comparable name string.
        /// </summary>
        /// <param name="name">Name of the book.</param>
        /// <param name="seriesName">Name of the book series.</param>
        /// <param name="index">Index of the book in the series.</param>
        /// <returns>The book name as a string.</returns>
        private static string GetComparableName(string? name, string? seriesName = null, int? index = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                if (!string.IsNullOrWhiteSpace(seriesName) && index != null)
                {
                    // We have series name and index, so use that
                    name = $"{BookFileNameParser.GetComparableString(seriesName, false)} {index}";
                }
                else
                {
                    return string.Empty;
                }
            }

            return BookFileNameParser.GetComparableString(name, true);
        }
    }
}
