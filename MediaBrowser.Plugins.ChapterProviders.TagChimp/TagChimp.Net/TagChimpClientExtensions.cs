using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace TagChimp
{
    public static class TagChimpClientExtensions
    {
        public static Movie FindById(this ITagChimpClient client, int id)
        {
            SearchParameters p = new SearchParameters
            {
                Id = id,
                Type = SearchType.Lookup
            };
            var results = client.Search(p);
            return results.Movies.FirstOrDefault();
        }

        public static SearchResults Search(this ITagChimpClient client, string query)
        {
            SearchParameters p = new SearchParameters
            {
                Type = SearchType.Search,
                Title = query
            };
            return client.Search(p);
        }

        public static SearchResults SearchTelevision(this ITagChimpClient client, string showName, string episodeName, int? season, int? episode)
        {
            SearchParameters p = new SearchParameters
            {
                Type = SearchType.Search,
                VideoKind = VideoKind.TVShow,
                Title = episodeName,
                Show = showName,
                Season = season,
                Episode = episode
            };
            return client.Search(p);
        }

        public static SearchResults SearchTelevisionExact(this ITagChimpClient client, string showName, string episodeName, int? season, int? episode)
        {
            SearchParameters p = new SearchParameters
            {
                //Type = SearchType.Search,
                //VideoKind = VideoKind.TVShow,
                Title = episodeName,
                Show = showName,
                Season = season,
                Episode = episode
            };
            
            var movies = from m in client.Search(p).Movies
                   where m.MovieTags.TelevisionInfo.ShowName == showName
                   select m;
            return new SearchResults
            {
                TotalResults = movies.Count(),
                Movies = movies.ToList()
            };
        }

        public static string BuildQueryString(this SearchParameters parameters)
        {
            ValidateParameters(parameters);

            if (String.IsNullOrEmpty(parameters.TotalChapters))
                parameters.TotalChapters = "X";

            Dictionary<string, string> items = new Dictionary<string, string>();
            items.AddValue("token", parameters.Token);
            items.AddValue("type", parameters.Type.ToString().ToLower());
            items.AddValue("title", parameters.Title);
            items.AddValue("totalChapters", parameters.TotalChapters);
            items.AddValue("id", parameters.Id);
            items.AddValue("lang", parameters.Language);
            items.AddValue("uid", parameters.UserId);
            items.AddValue("limit", parameters.Limit);
            items.AddValue("locked", parameters.Locked.ToString().ToLower());
            items.AddValue("amazon", parameters.Asin);
            items.AddValue("imdb", parameters.ImdbId);
            items.AddValue("netflix", parameters.NetflixId);
            items.AddValue("gtin", parameters.Gtin);
            items.AddValue("show", parameters.Show);
            items.AddValue("season", parameters.Season);
            items.AddValue("episode", parameters.Episode);
            if(parameters.VideoKind != null)
                items.AddValue("videoKind", parameters.VideoKind.ToLower());

            List<string> pairs = new List<string>();
            foreach (var pair in items)
            {
                if(!String.IsNullOrEmpty(pair.Key))
                    pairs.Add(String.Format("{0}={1}", System.Net.WebUtility.UrlEncode(pair.Key), System.Net.WebUtility.UrlEncode(pair.Value)));
            }
            return String.Join("&", pairs.ToArray());
        }

        private static void AddValue(this Dictionary<string, string> dictionary, string key, string value)
        {
            if (!String.IsNullOrEmpty(value))
                dictionary[key] = value;
        }

        private static void AddValue<T>(this Dictionary<string, string> dictionary, string key, Nullable<T> value)
            where T : struct
        {
            if (value.HasValue)
            {
                dictionary[key] = value.Value.ToString();
            }
        }

        private static void ValidateParameters(SearchParameters parameters)
        {
            if (String.IsNullOrEmpty(parameters.Token))
                throw new ArgumentNullException("Token");
            if (parameters.Type != SearchType.Search && parameters.Type != SearchType.Lookup)
                throw new ArgumentOutOfRangeException("Type");
        }
    }
}