using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers.Search
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

        public async Task<string> Search(BaseItem item, CancellationToken cancellationToken)
        {
            var imdbId = item.GetProviderId(MetadataProviders.Imdb);

            var url = string.Format("http://www.movie-list.com/trailers.php?id={0}", GetSearchName(item));

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
                        return null;
                    }

                    const string pattern = "HREF=\"(?<trailer>(.*?))\\.mov\"";

                    var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);

                    for (var i = 0; i < matches.Count; i++)
                    {
                        return WebUtility.HtmlDecode(matches[i].Groups["trailer"].Value + ".mov");
                    }
                }
            }

            return null;
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
