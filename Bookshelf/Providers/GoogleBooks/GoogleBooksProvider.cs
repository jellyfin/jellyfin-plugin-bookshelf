using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MBBookshelf.Providers.GoogleBooks
{
    class GoogleBooksProvider : BaseMetadataProvider
    {
        // Should move these constants elsewhere if I'm reusing them
        private const string StandardOpfFile = "content.opf";
        private const string CalibreOpfFile = "metadata.opf";
        private const string ComicRackMetaFile = "ComicInfo.xml";

        private static readonly Regex[] NameMatches = new[] {
            new Regex(@"(?<name>.*)\((?<year>\d{4}\))"), // matches "My book (2001)" and gives us the name and the year
            new Regex(@"(?<name>.*)") // last resort matches the whole string as the name
        };
        
        private static IHttpClient _httpClient;
        private static IJsonSerializer _jsonSerializer;
        private static ILogger _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logManager"></param>
        /// <param name="configurationManager"></param>
        /// <param name="httpClient"></param>
        /// <param name="jsonSerializer"></param>
        public GoogleBooksProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IHttpClient httpClient, IJsonSerializer jsonSerializer)
            : base(logManager, configurationManager)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _logger = logManager.GetLogger("MB Bookshelf");
        }

        #region BaseMetadataProvider

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override bool Supports(BaseItem item)
        {
            return item is Book;
        }

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            //if (HasLocalMeta(item))
            //    return false;

            //return base.NeedsRefreshInternal(item, providerInfo);
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="force"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            if (HasLocalMeta(item))
            {
                SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
                return true;
            }

            //await Fetch(item, cancellationToken).ConfigureAwait(false);

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Second; }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool RequiresInternet
        {
            get { return true; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected override DateTime CompareDate(BaseItem item)
        {
            return item.DateModified;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override string ProviderVersion
        {
            get
            {
                return "GoogleBooks Provider version 1.04";
            }
        }

        protected override bool RefreshOnVersionChange
        {
            get { return true; }
        }

        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool HasLocalMeta(BaseItem item)
        {
            return item.ResolveArgs.ContainsMetaFileByName(StandardOpfFile) ||
                   item.ResolveArgs.ContainsMetaFileByName(CalibreOpfFile) ||
                   item.ResolveArgs.ContainsMetaFileByName(ComicRackMetaFile);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<bool> Fetch(BaseItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var googleBookId = item.GetProviderId("GoogleBooks") ??
                await FetchBookId(item, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(googleBookId))
                return false;

            var bookResult = await FetchBookData(googleBookId, cancellationToken);

            if (bookResult == null)
                return false;

            ProcessBookData(item, bookResult, cancellationToken);

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<string> FetchBookId(BaseItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var name = item.Name;
            var year = string.Empty;

            foreach (var re in NameMatches)
            {
                Match m = re.Match(name);
                if (m.Success)
                {
                    name = m.Groups["name"].Value.Trim();
                    year = m.Groups["year"] != null ? m.Groups["year"].Value : null;
                    break;
                }
            }

            if (string.IsNullOrEmpty(year) && item.ProductionYear != null)
            {
                year = item.ProductionYear.ToString();
            }


            var url = string.Format(GoogleApiUrls.SearchUrl, UrlEncode(name), 0, 20);

            var stream = await _httpClient.Get(url, Plugin.Instance.GoogleBooksSemiphore, cancellationToken);

            if (stream == null)
            {
                _logger.Info("response is null");
                return null;
            }

            var searchResults = _jsonSerializer.DeserializeFromStream<SearchResult>(stream);

            if (searchResults == null || searchResults.items == null)
                return null;

            var comparableName = GetComparableName(item.Name);

            foreach (var i in searchResults.items)
            {
                if (!GetComparableName(i.volumeInfo.title).Equals(comparableName)) continue; // didn't match, move on to the next item

                if (!string.IsNullOrEmpty(year))
                {
                    // Need to adjust for googles format yyyy-mm-dd
                    var resultYear = i.volumeInfo.publishedDate.Length > 4 ? i.volumeInfo.publishedDate.Substring(0,4) : i.volumeInfo.publishedDate;

                    int bookReleaseYear;
                    if (Int32.TryParse(resultYear, out bookReleaseYear))
                    {
                        int localReleaseYear;
                        if (Int32.TryParse(year, out localReleaseYear))
                        {
                            if (Math.Abs(bookReleaseYear - localReleaseYear) > 1) // Allow a 1 year variance
                            {
                                continue;
                            }
                        }
                    }
                }

                // We have our match
                return i.id;
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="googleBookId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<BookResult> FetchBookData(string googleBookId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.Format(GoogleApiUrls.DetailsUrl, googleBookId);

            var stream = await _httpClient.Get(url, Plugin.Instance.GoogleBooksSemiphore, cancellationToken);

            if (stream == null)
            {
                _logger.Info("response is null");
                return null;
            }

            return _jsonSerializer.DeserializeFromStream<BookResult>(stream);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="bookResult"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private void ProcessBookData(BaseItem item, BookResult bookResult, CancellationToken cancellationToken)
        {
            var book = item as Book;
            cancellationToken.ThrowIfCancellationRequested();

            book.Name = bookResult.volumeInfo.title;
            book.Overview = bookResult.volumeInfo.description;
            try
            {
                book.ProductionYear = bookResult.volumeInfo.publishedDate.Length > 4
                                      ? Convert.ToInt32(bookResult.volumeInfo.publishedDate.Substring(0, 4))
                                      : Convert.ToInt32(bookResult.volumeInfo.publishedDate);
            }
            catch (Exception e)
            {
                _logger.ErrorException("Error parsing date", e);
            }

            if (!string.IsNullOrEmpty(bookResult.volumeInfo.publisher))
                book.Studios.Add(bookResult.volumeInfo.publisher);

            if (!string.IsNullOrEmpty(bookResult.volumeInfo.mainCatagory))
                book.Tags.Add(bookResult.volumeInfo.mainCatagory);
            
            if (bookResult.volumeInfo.catagories != null && bookResult.volumeInfo.catagories.Count > 0)
            {
                foreach (var catagory in bookResult.volumeInfo.catagories)
                    book.Tags.Add(catagory);
            }

            book.CommunityRating = bookResult.volumeInfo.averageRating * 2; // Google rates out of 5, not 10

            if (!string.IsNullOrEmpty(bookResult.id))
                book.SetProviderId("GoogleBooks", bookResult.id);
        }

        private const string Remove = "\"'!`?";
        // "Face/Off" support.
        private const string Spacers = "/,.:;\\(){}[]+-_=–*"; // (there are not actually two - they are different char codes)

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static string GetComparableName(string name)
        {
            name = name.ToLower();
            name = name.Normalize(NormalizationForm.FormKD);

            foreach (var pair in ReplaceEndNumerals)
            {
                if (name.EndsWith(pair.Key))
                {
                    name = name.Remove(name.IndexOf(pair.Key, StringComparison.InvariantCulture), pair.Key.Length);
                    name = name + pair.Value;
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

            string prevName;
            do
            {
                prevName = name;
                name = name.Replace("  ", " ");
            } while (name.Length != prevName.Length);

            return name.Trim();
        }

        /// <summary>
        /// 
        /// </summary>
        static readonly Dictionary<string, string> ReplaceEndNumerals = new Dictionary<string, string> {
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

        /// <summary>
        /// Encodes a text string
        /// </summary>
        /// <param name="name">the text to encode</param>
        /// <returns>a url safe string</returns>
        private static string UrlEncode(string name)
        {
            return WebUtility.UrlEncode(name);
        }

    }
}
