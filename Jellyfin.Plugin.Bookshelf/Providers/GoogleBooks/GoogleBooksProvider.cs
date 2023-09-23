using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
        // convert these characters to whitespace for better matching
        // there are two dashes with different char codes
        private const string Spacers = "/,.:;\\(){}[]+-_=–*";

        private const string Remove = "\"'!`?";

        private static readonly Regex[] _nameMatches =
        {
            // seriesName (seriesYear) #index (of count) (year), with only seriesName and index required
            new Regex(@"^(?<seriesName>.+?)((\s\((?<seriesYear>\d{4})\))?)\s#(?<index>\d+)((\s\(of\s(?<count>\d+)\))?)((\s\((?<year>\d{4})\))?)$"),
            // name (seriesName, #index) (year), with year optional
            new Regex(@"^(?<name>.+?)\s\((?<seriesName>.+?),\s#(?<index>\d+)\)((\s\((?<year>\d{4})\))?)$"),
            // index - name (year), with year optional
            new Regex(@"^(?<index>\d+)\s\-\s(?<name>.+?)((\s\((?<year>\d{4})\))?)$"),
            // name (year)
            new Regex(@"(?<name>.*)\((?<year>\d{4})\)"),
            // last resort matches the whole string as the name
            new Regex(@"(?<name>.*)")
        };

        private readonly Dictionary<string, string> _replaceEndNumerals = new ()
        {
            { " i", " 1" },
            { " ii", " 2" },
            { " iii", " 3" },
            { " iv", " 4" },
            { " v", " 5" },
            { " vi", " 6" },
            { " vii", " 7" },
            { " viii", " 8" },
            { " ix", " 9" },
            { " x", " 10" }
        };

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
            GetBookMetadata(item);

            var searchResults = await GetSearchResultsInternal(item, cancellationToken)
                .ConfigureAwait(false);
            if (searchResults?.Items == null)
            {
                return null;
            }

            var comparableName = GetComparableName(item.Name, item.SeriesName, item.IndexNumber);
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
                if (Math.Abs(resultYear - item.Year ?? 0) > 1)
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
                    Type = "Author",
                });
            }

            if (!string.IsNullOrWhiteSpace(bookResult.VolumeInfo.Language))
            {
                metadataResult.ResultLanguage = bookResult.VolumeInfo.Language;
            }
        }

        private string GetComparableName(string? name, string? seriesName = null, int? index = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                if (!string.IsNullOrWhiteSpace(seriesName) && index != null)
                {
                    // We searched by series name and index, so use that
                    name = $"{seriesName} {index}";
                }
                else
                {
                    return string.Empty;
                }
            }

            name = name.ToLower(CultureInfo.InvariantCulture);
            name = name.Normalize(NormalizationForm.FormC);

            foreach (var pair in _replaceEndNumerals)
            {
                if (name.EndsWith(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Remove(name.IndexOf(pair.Key, StringComparison.InvariantCulture), pair.Key.Length);
                    name += pair.Value;
                }
            }

            var sb = new StringBuilder();
            foreach (var c in name)
            {
                if (c >= 0x2B0 && c <= 0x0333)
                {
                    // skip char modifier and diacritics
                }
                else if (Remove.IndexOf(c, StringComparison.Ordinal) > -1)
                {
                    // skip chars we are removing
                }
                else if (Spacers.IndexOf(c, StringComparison.Ordinal) > -1)
                {
                    sb.Append(' ');
                }
                else if (c == '&')
                {
                    sb.Append(" and ");
                }
                else
                {
                    sb.Append(c);
                }
            }

            name = sb.ToString();
            name = name.Replace("the", " ", StringComparison.OrdinalIgnoreCase);
            name = name.Replace(" - ", ": ", StringComparison.Ordinal);

            var regex = new Regex(@"\s+");
            name = regex.Replace(name, " ");

            return name.Trim();
        }

        /// <summary>
        /// Extract metadata from the file name.
        /// </summary>
        /// <param name="item">The info item.</param>
        internal void GetBookMetadata(BookInfo item)
        {
            foreach (var regex in _nameMatches)
            {
                var match = regex.Match(item.Name);
                if (!match.Success)
                {
                    continue;
                }

                // Reset the name, since we'll get it from parsing
                item.Name = string.Empty;

                if (item.SeriesName == CollectionType.Books)
                {
                    // If the book is in a folder, the folder's name will be set as the series name
                    // And we'll override it if we find it in the file name
                    // If it's not in a folder, the series name will be set to the name of the collection
                    // In this case reset it so it's not included in the search string
                    item.SeriesName = string.Empty;
                }

                // catch return value because user may want to index books from zero
                // but zero is also the return value from int.TryParse failure
                var result = int.TryParse(match.Groups["index"].Value, out var index);
                if (result)
                {
                    item.IndexNumber = index;
                }

                if (match.Groups.TryGetValue("name", out Group? nameGroup))
                {
                    item.Name = nameGroup.Value.Trim();
                }

                if (match.Groups.TryGetValue("seriesName", out Group? seriesGroup))
                {
                    item.SeriesName = seriesGroup.Value.Trim();
                }

                // might as well catch the return value here as well
                result = int.TryParse(match.Groups["year"].Value, out var year);
                if (result)
                {
                    item.Year = year;
                }
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
    }
}
