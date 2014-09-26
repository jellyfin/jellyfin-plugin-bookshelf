using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.LocalTrailers.Search
{
    public class MovieListSearch
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;

        public MovieListSearch(IHttpClient httpClient, IJsonSerializer json)
        {
            _httpClient = httpClient;
            _json = json;
        }

        public async Task<List<string>> Search(BaseItem item, CancellationToken cancellationToken)
        {
            var imdbId = item.GetProviderId(MetadataProviders.Imdb);

            var url = string.Format("http://www.movie-list.com/trailers.php?id={0}", GetSearchName(item));

            var results = new List<string>();

            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken
            }))
            {
                using (var reader = new StreamReader(stream))
                {
                    var html = await reader.ReadToEndAsync().ConfigureAwait(false);

                    if (html.IndexOf(string.Format("imdb.com/title/{0}", imdbId), StringComparison.Ordinal) == -1)
                    {
                        return results;
                    }

                    const string hrefPattern = "HREF=\"(?<trailer>(.*?))\\.mov\"";
                    var matches = Regex.Matches(html, hrefPattern, RegexOptions.IgnoreCase);

                    for (var i = 0; i < matches.Count; i++)
                    {
                        var match = WebUtility.HtmlDecode(matches[i].Groups["trailer"].Value + ".mov");

                        if (!string.IsNullOrEmpty(match))
                        {
                            results.Add(match);
                        }
                    }

                    const string mp4Pattern = "file: \"(?<trailer>(.*?))\\.mp4\"";
                    matches = Regex.Matches(html, mp4Pattern, RegexOptions.IgnoreCase);

                    for (var i = 0; i < matches.Count; i++)
                    {
                        var match = WebUtility.HtmlDecode(matches[i].Groups["trailer"].Value + ".mp4");

                        if (!string.IsNullOrEmpty(match))
                        {
                            results.Add(match);
                        }
                    }

                }
            }

            return results.OrderBy(i =>
            {
                if (i.IndexOf("1080", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return 0;
                }
                if (i.IndexOf("720", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return 1;
                }
                if (i.IndexOf("480", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return 2;
                }

                return 3;

            })
            .ToList();
        }

        private string GetSearchName(BaseItem item)
        {
            var name = HdNetTrailerSearch.GetSearchTitle(item.Name).ToLower().Replace(" ", string.Empty).Trim().Replace(",the", string.Empty).Replace(",a", string.Empty);

            return RemoveDiacritics(name);
        }

        private string RemoveDiacritics(string text)
        {
            return string.Concat(
                text.Normalize(NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) !=
                                              UnicodeCategory.NonSpacingMark)
              ).Normalize(NormalizationForm.FormC);
        }
    }
}
