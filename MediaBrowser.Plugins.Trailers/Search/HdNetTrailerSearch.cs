using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers.Search
{
    /// <summary>
    /// http://hdtrailersdler.codeplex.com/
    /// </summary>
    public class HdNetTrailerSearch
    {
        private readonly IHttpClient _httpClient;

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        public HdNetTrailerSearch(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> Search(BaseItem item, CancellationToken cancellationToken)
        {
            const string urlFormat = "http://www.hd-trailers.net/Library/{0}/";

            var section = GetSearchTitle(item.Name).Substring(0, 1);
            int sectionNumber;

            if (int.TryParse(section, NumberStyles.Integer, UsCulture, out sectionNumber))
            {
                section = "#";
            }

            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = string.Format(urlFormat, section),
                CancellationToken = cancellationToken
            }))
            {
                using (var reader = new StreamReader(stream))
                {
                    var html = await reader.ReadToEndAsync().ConfigureAwait(false);

                    var match = Regex.Match(html, "<td class=\"trailer\"><a href=\"(?<url>.*?)\">" + Escape(WebUtility.HtmlEncode(item.Name)) + "</a></td>", RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        var url = "http://www.hd-trailers.net" + match.Groups["url"].Value;

                        return await GetTrailerFromPage(url, cancellationToken).ConfigureAwait(false);
                    }

                    return null;
                }
            }
        }

        private async Task<string> GetTrailerFromPage(string url, CancellationToken cancellationToken)
        {
            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken
            }))
            {
                using (var reader = new StreamReader(stream))
                {
                    var html = await reader.ReadToEndAsync().ConfigureAwait(false);

                    const string regexFormat = "<a href=\"(?<trailer>.*?)\" rel=\"lightbox\\[res{0}";

                    var matchCollection = Regex.Matches(html, string.Format(regexFormat, "1080p"), RegexOptions.IgnoreCase);

                    if (matchCollection.Count == 0)
                    {
                        matchCollection = Regex.Matches(html, string.Format(regexFormat, "720p"), RegexOptions.IgnoreCase);
                    }

                    if (matchCollection.Count == 0)
                    {
                        matchCollection = Regex.Matches(html, string.Format(regexFormat, "480p"), RegexOptions.IgnoreCase);
                    }

                    for (var i = 0; i < matchCollection.Count; i++)
                    {
                        var val = WebUtility.HtmlDecode(matchCollection[0].Groups["trailer"].Value);

                        if (val.IndexOf("yahoo", StringComparison.OrdinalIgnoreCase) == -1)
                        {
                            return WebUtility.HtmlDecode(matchCollection[0].Groups["trailer"].Value);
                        }
                    }

                    return null;
                }
            }
        }

        internal static string GetSearchTitle(string name)
        {
            var ignoreWords = new[] {
                "The",
	            "A",
	            "An"
            };

            var num = ignoreWords.Length - 1;
            for (var i = 0; i <= num; i++)
            {
                if (name.StartsWith(ignoreWords[i] + " ", StringComparison.OrdinalIgnoreCase))
                {
                    return name.Remove(0, ignoreWords[i].Length + 1).Trim() + ", " + ignoreWords[i];
                }
            }
            return name;
        }

        internal static string Escape(string text)
        {
            var array = new[]
	            {
		            '[',
		            '\\',
		            '^',
		            '$',
		            '.',
		            '|',
		            '?',
		            '*',
		            '+',
		            '(',
		            ')'
	            };

            var stringBuilder = new StringBuilder();
            var i = 0;
            var length = text.Length;

            while (i < length)
            {
                var character = text[i];

                if (Array.IndexOf(array, character) != -1)
                {
                    stringBuilder.Append("\\" + character.ToString());
                }
                else
                {
                    stringBuilder.Append(character);
                }
                i++;
            }
            return stringBuilder.ToString();
        }

    }
}
