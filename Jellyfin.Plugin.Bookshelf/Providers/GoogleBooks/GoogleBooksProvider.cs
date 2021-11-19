using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// <summary>
    /// Google books provider.
    /// </summary>
    public class GoogleBooksProvider : IRemoteMetadataProvider<Book, BookInfo>
    {
        // convert these characters to whitespace for better matching
        // there are two dashes with different char codes
        private const string Spacers = "/,.:;\\(){}[]+-_=–*";

        private const string Remove = "\"'!`?";

        // first pattern provides the name and the year
        // alternate option to use series index instead of year
        // last resort matches the whole string as the name
        private static readonly Regex[] _nameMatches =
        {
            new Regex(@"(?<name>.*)\((?<year>\d{4})\)"),
            new Regex(@"(?<index>\d*)\s\-\s(?<name>.*)"),
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
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "Google Books";

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BookInfo searchInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var list = new List<RemoteSearchResult>();

            var searchResults = await GetSearchResultsInternal(searchInfo, cancellationToken).ConfigureAwait(false);
            if (searchResults is null)
            {
                return Enumerable.Empty<RemoteSearchResult>();
            }

            foreach (var result in searchResults.Items)
            {
                if (result.VolumeInfo is null)
                {
                    continue;
                }

                var remoteSearchResult = new RemoteSearchResult();
                remoteSearchResult.Name = result.VolumeInfo.Title;
                if (result.VolumeInfo.ImageLinks?.Thumbnail != null)
                {
                    remoteSearchResult.ImageUrl = result.VolumeInfo.ImageLinks.Thumbnail;
                }

                list.Add(remoteSearchResult);
            }

            return list;
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Book>> GetMetadata(BookInfo info, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var metadataResult = new MetadataResult<Book>();
            metadataResult.HasMetadata = false;

            var googleBookId = info.GetProviderId("GoogleBooks")
                               ?? await FetchBookId(info, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(googleBookId))
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

            metadataResult.Item = bookMetadataResult;
            metadataResult.QueriedById = true;
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

            // pattern match the filename
            // year can be included for better results
            GetBookMetadata(item);

            var url = string.Format(CultureInfo.InvariantCulture, GoogleApiUrls.SearchUrl, WebUtility.UrlEncode(item.Name), 0, 20);

            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);

            using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            #pragma warning disable CA2007
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            #pragma warning restore CA2007

            return await JsonSerializer.DeserializeAsync<SearchResult>(stream, JsonDefaults.Options, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string?> FetchBookId(BookInfo item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var searchResults = await GetSearchResultsInternal(item, cancellationToken)
                .ConfigureAwait(false);
            if (searchResults?.Items == null)
            {
                return null;
            }

            var comparableName = GetComparableName(item.Name);
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
                var resultYear = i.VolumeInfo.PublishedDate?.Length > 4 ? i.VolumeInfo.PublishedDate[..4] : i.VolumeInfo.PublishedDate;
                if (!int.TryParse(resultYear, out var bookReleaseYear))
                {
                    continue;
                }

                // allow a one year variance
                if (Math.Abs(bookReleaseYear - item.Year ?? 0) > 1)
                {
                    continue;
                }

                return i.Id;
            }

            return null;
        }

        private async Task<BookResult?> FetchBookData(string googleBookId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.Format(CultureInfo.InvariantCulture, GoogleApiUrls.DetailsUrl, googleBookId);

            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);

            using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            #pragma warning disable CA2007
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            #pragma warning restore CA2007

            return await JsonSerializer.DeserializeAsync<BookResult>(stream, JsonDefaults.Options, cancellationToken).ConfigureAwait(false);
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
            book.Overview = bookResult.VolumeInfo.Description;
            try
            {
                book.ProductionYear = bookResult.VolumeInfo.PublishedDate?.Length > 4
                    ? Convert.ToInt32(bookResult.VolumeInfo.PublishedDate[..4], CultureInfo.InvariantCulture)
                    : Convert.ToInt32(bookResult.VolumeInfo.PublishedDate, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                _logger.LogError("Error parsing date");
            }

            if (!string.IsNullOrEmpty(bookResult.VolumeInfo.Publisher))
            {
                book.Studios = book.Studios.Append(bookResult.VolumeInfo.Publisher).ToArray();
            }

            var tags = new List<string>();
            if (!string.IsNullOrEmpty(bookResult.VolumeInfo.MainCategory))
            {
                tags.Add(bookResult.VolumeInfo.MainCategory);
            }

            if (bookResult.VolumeInfo.Categories is { Count: > 0 })
            {
                foreach (var category in bookResult.VolumeInfo.Categories)
                {
                    tags.Add(category);
                }
            }

            if (tags.Count > 0)
            {
                tags.AddRange(book.Tags);
                book.Tags = tags.ToArray();
            }

            // google rates out of five so convert to ten
            book.CommunityRating = bookResult.VolumeInfo.AverageRating * 2;

            if (!string.IsNullOrEmpty(bookResult.Id))
            {
                book.SetProviderId("GoogleBooks", bookResult.Id);
            }

            return book;
        }

        private string GetComparableName(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            name = name.ToLower(CultureInfo.InvariantCulture);
            name = name.Normalize(NormalizationForm.FormKD);

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

        private void GetBookMetadata(BookInfo item)
        {
            foreach (var regex in _nameMatches)
            {
                var match = regex.Match(item.Name);
                if (!match.Success)
                {
                    continue;
                }

                // catch return value because user may want to index books from zero
                // but zero is also the return value from int.TryParse failure
                var result = int.TryParse(match.Groups["index"].Value, out var index);
                if (result)
                {
                    item.IndexNumber = index;
                }

                item.Name = match.Groups["name"].Value.Trim();

                // might as well catch the return value here as well
                result = int.TryParse(match.Groups["year"].Value, out var year);
                if (result)
                {
                    item.Year = year;
                }
            }
        }
    }
}
