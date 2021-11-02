using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    public class GoogleBooksProvider : IRemoteMetadataProvider<Book, BookInfo>
    {
        // first pattern provides the name and the year
        // alternate option to use series index instead of year
        // last resort matches the whole string as the name
        private static readonly Regex[] NameMatches = new[] {
            new Regex(@"(?<name>.*)\((?<year>\d{4})\)"),
            new Regex(@"(?<index>\d*)\s\-\s(?<name>.*)"),
            new Regex(@"(?<name>.*)")
        };

        private IHttpClientFactory _httpClientFactory;
        private ILogger<GoogleBooksProvider> _logger;

        public GoogleBooksProvider(ILogger<GoogleBooksProvider> logger, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public string Name => "Google Books";

        public bool Supports(BaseItem item)
        {
            return item is Book;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BookInfo item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var list = new List<RemoteSearchResult>();

            var searchResults = await GetSearchResultsInternal(item, cancellationToken);
            foreach (var result in searchResults.items)
            {
                var remoteSearchResult = new RemoteSearchResult();
                remoteSearchResult.Name = result.volumeInfo.title;
                if (result.volumeInfo.imageLinks?.thumbnail != null)
                {
                    remoteSearchResult.ImageUrl = result.volumeInfo.imageLinks.thumbnail;
                }

                list.Add(remoteSearchResult);
            }

            return list;
        }

        public async Task<MetadataResult<Book>> GetMetadata(BookInfo item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MetadataResult<Book> metadataResult = new MetadataResult<Book>();
            metadataResult.HasMetadata = false;

            var googleBookId = item.GetProviderId("GoogleBooks")
                ?? await FetchBookId(item, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(googleBookId))
            {
                return metadataResult;
            }

            var bookResult = await FetchBookData(googleBookId, cancellationToken);

            if (bookResult == null)
            {
                return metadataResult;
            }

            metadataResult.Item = ProcessBookData(bookResult, cancellationToken);
            metadataResult.QueriedById = true;
            metadataResult.HasMetadata = true;
            return metadataResult;
        }

        private async Task<SearchResult> GetSearchResultsInternal(BookInfo item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // pattern match the filename
            // year can be included for better results
            GetBookMetadata(item);

            var url = string.Format(GoogleApiUrls.SearchUrl, WebUtility.UrlEncode(item.Name), 0, 20);

            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);

            using (var response = await httpClient.GetAsync(url).ConfigureAwait(false))
            {
                await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return await JsonSerializer.DeserializeAsync<SearchResult>(stream, JsonDefaults.Options).ConfigureAwait(false);
            }

        }

        private async Task<string> FetchBookId(BookInfo item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var searchResults = await GetSearchResultsInternal(item, cancellationToken);
            if (searchResults?.items == null)
            {
                return null;
            }

            var comparableName = GetComparableName(item.Name);
            foreach (var i in searchResults.items)
            {
                // no match so move on to the next item
                if (!GetComparableName(i.volumeInfo.title).Equals(comparableName)) continue;

                // adjust for google yyyy-mm-dd format
                var resultYear = i.volumeInfo.publishedDate.Length > 4 ? i.volumeInfo.publishedDate.Substring(0,4) : i.volumeInfo.publishedDate;
                if (!int.TryParse(resultYear, out var bookReleaseYear)) continue;

                // allow a one year variance
                if (Math.Abs(bookReleaseYear - item.Year ?? 0) > 1) continue;

                return i.id;
            }

            return null;
        }

        private async Task<BookResult> FetchBookData(string googleBookId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.Format(GoogleApiUrls.DetailsUrl, googleBookId);

            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);

            using (var response = await httpClient.GetAsync(url).ConfigureAwait(false))
            {
                await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return await JsonSerializer.DeserializeAsync<BookResult>(stream, JsonDefaults.Options).ConfigureAwait(false);
            }
        }

        private Book ProcessBookData(BookResult bookResult, CancellationToken cancellationToken)
        {
            var book = new Book();
            cancellationToken.ThrowIfCancellationRequested();

            book.Name = bookResult.volumeInfo.title;
            book.Overview = bookResult.volumeInfo.description;
            try
            {
                book.ProductionYear = bookResult.volumeInfo.publishedDate.Length > 4
                    ? Convert.ToInt32(bookResult.volumeInfo.publishedDate.Substring(0, 4))
                    : Convert.ToInt32(bookResult.volumeInfo.publishedDate);
            }
            catch (Exception)
            {
                _logger.LogError("Error parsing date");
            }

            if (!string.IsNullOrEmpty(bookResult.volumeInfo.publisher))
                book.Studios.Append(bookResult.volumeInfo.publisher);

            if (!string.IsNullOrEmpty(bookResult.volumeInfo.mainCatagory))
                book.Tags.Append(bookResult.volumeInfo.mainCatagory);

            if (bookResult.volumeInfo.catagories != null && bookResult.volumeInfo.catagories.Count > 0)
            {
                foreach (var category in bookResult.volumeInfo.catagories)
                    book.Tags.Append(category);
            }

            // google rates out of five so convert to ten
            book.CommunityRating = bookResult.volumeInfo.averageRating * 2;

            if (!string.IsNullOrEmpty(bookResult.id))
                book.SetProviderId("GoogleBooks", bookResult.id);

            return book;
        }

        // convert these characters to whitespace for better matching
        // there are two dashes with different char codes
        private const string Spacers = "/,.:;\\(){}[]+-_=–*";
        private const string Remove = "\"'!`?";

        private string GetComparableName(string name)
        {
            name = name.ToLower();
            name = name.Normalize(NormalizationForm.FormKD);

            foreach (var pair in _replaceEndNumerals)
            {
                if (name.EndsWith(pair.Key))
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
                else if (Remove.IndexOf(c) > -1)
                {
                    // skip chars we are removing
                }
                else if (Spacers.IndexOf(c) > -1)
                {
                    sb.Append(" ");
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
            name = name.Replace("the", " ");
            name = name.Replace(" - ", ": ");

            Regex regex = new Regex(@"\s+");
            name = regex.Replace(name, " ");

            return name.Trim();
        }

        private void GetBookMetadata(BookInfo item)
        {
            foreach (var regex in NameMatches)
            {
                var match = regex.Match(item.Name);
                if (!match.Success) continue;

                // catch return value because user may want to index books from zero
                // but zero is also the return value from int.TryParse failure
                var result = int.TryParse(match.Groups["index"]?.Value, out var index);
                if (result) item.IndexNumber = index;

                item.Name = match.Groups["name"].Value.Trim();

                // might as well catch the return value here as well
                result = int.TryParse(match.Groups["year"]?.Value, out var year);
                if (result) item.Year = year;
            }
        }

        private readonly Dictionary<string, string> _replaceEndNumerals = new Dictionary<string, string> {
            {" i", " 1"},
            {" ii", " 2"},
            {" iii", " 3"},
            {" iv", " 4"},
            {" v", " 5"},
            {" vi", " 6"},
            {" vii", " 7"},
            {" viii", " 8"},
            {" ix", " 9"},
            {" x", " 10"}
        };

        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            return await httpClient.GetAsync(url).ConfigureAwait(false);
        }
    }
}
