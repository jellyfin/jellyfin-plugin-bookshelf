using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MBBookshelf.Providers.ComicVine
{
    public class ComicVineMetadataProvider : IRemoteMetadataProvider<Book, BookInfo>
    {
        private const string ApiKey = "cc632e23e4b370807f4de6f0e3ba0116c734c10b";
        private const string VolumeSearchUrl =
            @"http://api.comicvine.com/search/?api_key={0}&format=json&resources=issue&query={1}";

        private const string IssueSearchUrl =
            @"http://api.comicvine.com/issues/?api_key={0}&format=json&filter=issue_number:{1},volume:{2}";

        private static readonly Regex[] NameMatches =
        {
            new Regex(@"(?<name>.*)\((?<year>\d{4})\)"), // matches "My Comic (2001)" and gives us the name and the year
            new Regex(@"(?<name>.*)") // last resort matches the whole string as the name
        };

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IApplicationPaths _appPaths;

        public static ComicVineMetadataProvider Current;

        public ComicVineMetadataProvider(ILogger logger, IHttpClient httpClient, IJsonSerializer jsonSerializer, IFileSystem fileSystem, IApplicationPaths appPaths)
        {
            _logger = logger;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _appPaths = appPaths;
            Current = this;
        }

        public async Task<MetadataResult<Book>> GetMetadata(BookInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Book>();

            var volumeId = info.GetProviderId(ComicVineVolumeExternalId.KeyName) ??
                              await FetchComicVolumeId(info, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(volumeId))
            {
                return result;
            }

            var issueNumber = GetIssueNumberFromName(info.Name).ToString(_usCulture);

            await EnsureCacheFile(volumeId, issueNumber, cancellationToken).ConfigureAwait(false);

            var cachePath = GetCacheFilePath(volumeId, issueNumber);

            try
            {
                var issueInfo = _jsonSerializer.DeserializeFromFile<SearchResult>(cachePath);

                result.Item = new Book();
                result.Item.SetProviderId(ComicVineVolumeExternalId.KeyName, volumeId);
                result.HasMetadata = true;

                ProcessIssueData(result.Item, issueInfo, cancellationToken);
            }
            catch (FileNotFoundException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="issue"></param>
        /// <param name="cancellationToken"></param>
        private void ProcessIssueData(Book item, SearchResult issue, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (issue.results == null || issue.results.Count == 0)
                return;

            var name = issue.results[0].issue_number;

            if (!string.IsNullOrEmpty(issue.results[0].name))
                name += " - " + issue.results[0].name;

            item.Name = name;

            string sortIssueName = issue.results[0].issue_number;

            if (sortIssueName.Length == 1)
                sortIssueName = "00" + sortIssueName;
            else if (sortIssueName.Length == 2)
                sortIssueName = "0" + sortIssueName;

            sortIssueName += " - " + issue.results[0].volume.name;

            if (!string.IsNullOrEmpty(issue.results[0].name))
                sortIssueName += ", " + issue.results[0].name;

            item.ForcedSortName = sortIssueName;

            item.SeriesName = issue.results[0].volume.name;

            item.Overview = WebUtility.HtmlDecode(issue.results[0].description);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<string> FetchComicVolumeId(BookInfo item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            /*
             * Comics should be stored so that they represent the volume number and the parent represents the comic series.
             */
            var name = item.SeriesName;
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

            if (string.IsNullOrEmpty(year) && item.Year != null)
            {
                year = item.Year.ToString();
            }

            var url = string.Format(VolumeSearchUrl, ApiKey, UrlEncode(name));
            var stream = await _httpClient.Get(url, Plugin.Instance.ComicVineSemiphore, cancellationToken);

            if (stream == null)
            {
                _logger.Info("response is null");
                return null;
            }

            var searchResult = _jsonSerializer.DeserializeFromStream<SearchResult>(stream);

            var comparableName = GetComparableName(name);

            foreach (var result in searchResult.results)
            {
                if (result.volume.name != null &&
                    GetComparableName(result.volume.name).Equals(comparableName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Info("volume name: " + GetComparableName(result.volume.name) + ", matches: " + comparableName);
                    if (!string.IsNullOrEmpty(year))
                    {
                        var resultYear = result.cover_date.Substring(0, 4);

                        if (year == resultYear)
                            return result.volume.id.ToString(CultureInfo.InvariantCulture);
                    }
                    else
                        return result.volume.id.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    if (result.volume.name != null)
                        _logger.Info(comparableName + " does not match " + GetComparableName(result.volume.name));
                }
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="volumeId"></param>
        /// <param name="issueNumber"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<SearchResult> GetComicIssue(string volumeId, float issueNumber, CancellationToken cancellationToken)
        {
            var url = string.Format(IssueSearchUrl, ApiKey, issueNumber, volumeId);

            var stream = await _httpClient.Get(url, Plugin.Instance.ComicVineSemiphore, cancellationToken);

            if (stream == null)
            {
                _logger.Info("response is null");
                return null;
            }

            return _jsonSerializer.DeserializeFromStream<SearchResult>(stream);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static float GetIssueNumberFromName(string name)
        {
            var result = Regex.Match(name, @"\d+\.\d").Value;

            if (string.IsNullOrEmpty(result))
                result = Regex.Match(name, @"#\d+").Value;

            if (string.IsNullOrEmpty(result))
                result = Regex.Match(name, @"\d+").Value;

            if (!string.IsNullOrEmpty(result))
            {
                result = result.Replace("#", "");
                // Remove any leading zeros so that 005 becomes 5
                result = result.TrimStart(new[] { '0' });

                var issueNumber = float.Parse(result);
                return issueNumber;
            }

            return 0;
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

        private static string UrlEncode(string name)
        {
            return WebUtility.UrlEncode(name);
        }

        public string Name
        {
            get { return "Comic Vine"; }
        }

        private readonly Task _cachedResult = Task.FromResult(true);

        internal Task EnsureCacheFile(string volumeId, string issueNumber, CancellationToken cancellationToken)
        {
            var path = GetCacheFilePath(volumeId, issueNumber);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 7)
                {
                    return _cachedResult;
                }
            }

            return DownloadIssueInfo(volumeId, issueNumber, cancellationToken);
        }

        internal async Task DownloadIssueInfo(string volumeId, string issueNumber, CancellationToken cancellationToken)
        {
            var url = string.Format(IssueSearchUrl, ApiKey, issueNumber, volumeId);

            var xmlPath = GetCacheFilePath(volumeId, issueNumber);

            using (var stream = await _httpClient.Get(url, Plugin.Instance.ComicVineSemiphore, cancellationToken).ConfigureAwait(false))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(xmlPath));

                using (var fileStream = _fileSystem.GetFileStream(xmlPath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }
        }

        internal string GetCacheFilePath(string volumeId, string issueNumber)
        {
            var gameDataPath = GetComicVineDataPath();
            return Path.Combine(gameDataPath, volumeId, "issue-" + issueNumber.ToString(_usCulture) + ".json");
        }

        private string GetComicVineDataPath()
        {
            var dataPath = Path.Combine(_appPaths.CachePath, "comicvine");

            return dataPath;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BookInfo searchInfo, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
