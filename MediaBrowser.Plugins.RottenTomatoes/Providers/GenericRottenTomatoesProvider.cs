using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.RottenTomatoes.Providers
{
    public class GenericRottenTomatoesProvider<T>
        where T : BaseItem, new()
    {
        // http://developer.rottentomatoes.com/iodocs

        private const int DailyRefreshLimit = 1000;

        private const string MoviesReviews = @"movies/{1}/reviews.json?review_type=top_critic&page_limit=12&page=1&country=us&apikey={0}";

        private readonly string[] _apiKeys =
            {
                // MB Server key (listed 3x because we have a 30k limit)
                "x9wjnvv39ntjmt9zs95nm7bg",

                // MB Server key (listed 3x because we have a 30k limit)
                "x9wjnvv39ntjmt9zs95nm7bg",

                // MB Server key (listed 3x because we have a 30k limit)
                "x9wjnvv39ntjmt9zs95nm7bg",

                // Donated by Redshirt
                "gecbjvjka5may65qmqrczk97",

                // MB Theater
                //"4wku9pfehuvwrrt5fyjgbert",

                // MB Classic
                //"t579r22wuq9399ra8u7cevs7"
            };

        private const string BasicUrl = @"http://api.rottentomatoes.com/api/public/v1.0/";
        private const string MovieImdb = @"movie_alias.json?id={1}&type=imdb&apikey={0}";

        private readonly SemaphoreSlim _rottenTomatoesResourcePool = new SemaphoreSlim(1, 1);

        private readonly SemaphoreSlim _refreshResourcePool = new SemaphoreSlim(1, 1);
        
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private IApplicationPaths _appPaths;
        private IHttpClient _httpClient;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public GenericRottenTomatoesProvider(ILogger logger, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
        }   

        /// <summary>
        /// Gets the request history file path.
        /// </summary>
        /// <value>The request history file path.</value>
        private string RequestHistoryFilePath
        {
            get
            {
                return Path.Combine(Path.Combine(_appPaths.CachePath, "rotten-tomatoes"), "data.dat");
            }
        }

        public async Task<MetadataResult<T>> GetMetadata(ItemLookupInfo itemId, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<T>();

            await _refreshResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            var now = DateTime.UtcNow;

            var history = LoadRequestHistory();
            
            if (history.Count(i => (now - i).TotalDays <= 1) >= DailyRefreshLimit)
            {
                _refreshResourcePool.Release();

                _logger.Debug("Skipping {0} because daily request limit has been reached. Tomorrow's refresh will retrieve it.", item.Name);

                return result;
            }

            try
            {
                await FetchAsyncInternal(item, force, info, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                SaveRequestHistory(history);

                _refreshResourcePool.Release();
            }

            return result;
        }

        /// <summary>
        /// Loads the request history.
        /// </summary>
        /// <returns>List{DateTime}.</returns>
        private List<DateTime> LoadRequestHistory()
        {
            try
            {
                return
                    File.ReadAllText(RequestHistoryFilePath)
                        .Split('|')
                        .Select(i =>
                        {
                            long ticks;

                            if (long.TryParse(i, NumberStyles.Any, _usCulture, out ticks))
                            {
                                return new DateTime(ticks, DateTimeKind.Utc);
                            }

                            return DateTime.MinValue;
                        })
                        .ToList();
            }
            catch
            {
                return new List<DateTime>();
            }
        }

        private void SaveRequestHistory(IEnumerable<DateTime> history)
        {
            using (var fs = new FileStream(RequestHistoryFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (var streamWriter = new StreamWriter(fs))
                {
                    var now = DateTime.UtcNow;

                    var text = string.Join("|", history.Where(i => (now - i).TotalDays <= 2)
                        .Select(i => i.Ticks.ToString(_usCulture))
                        .ToArray());

                    streamWriter.Write(text);
                }
            }
        }

        /// <summary>
        /// Fetches the async internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="requestHistory">The request history.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        private async Task FetchAsyncInternal(ItemLookupInfo item, List<DateTime> requestHistory, CancellationToken cancellationToken)
        {
            var imdbId = item.GetProviderId(MetadataProviders.Imdb);

            if (string.IsNullOrEmpty(imdbId))
            {
                return;
            }

            var apiKey = GetApiKey();

            if (string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.RottenTomatoes)))
            {
                await FetchRottenTomatoesId(item, apiKey, requestHistory, cancellationToken).ConfigureAwait(false);
            }

            // If still empty we can't continue
            if (string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.RottenTomatoes)))
            {
                return;
            }

            requestHistory.Add(DateTime.UtcNow);

            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = GetMovieReviewsUrl(item.GetProviderId(MetadataProviders.RottenTomatoes), apiKey),
                ResourcePool = _rottenTomatoesResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                var result = _jsonSerializer.DeserializeFromStream<RTReviewList>(stream);

                var criticReviews = result.reviews.Select(rtReview => new ItemReview
                {
                    ReviewerName = rtReview.critic,
                    Publisher = rtReview.publication,
                    Date = DateTime.Parse(rtReview.date).ToUniversalTime(),
                    Caption = rtReview.quote,
                    Url = rtReview.links.review,
                    Likes = string.Equals(rtReview.freshness, "fresh", StringComparison.OrdinalIgnoreCase)

                }).ToList();

                //await _itemRepo.SaveCriticReviews(item.Id, criticReviews).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Fetches the rotten tomatoes id.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="requestHistory">The request history.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchRottenTomatoesId(ItemLookupInfo item, string apiKey, List<DateTime> requestHistory, CancellationToken cancellationToken)
        {
            var imdbId = item.GetProviderId(MetadataProviders.Imdb);

            requestHistory.Add(DateTime.UtcNow);
            
            // Have IMDB Id
            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = GetMovieImdbUrl(imdbId, apiKey),
                ResourcePool = _rottenTomatoesResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                var hit = _jsonSerializer.DeserializeFromStream<RTMovieSearchResult>(stream);

                if (!string.IsNullOrEmpty(hit.id))
                {
                    var hasCriticRating = item as IHasCriticRating;

                    if (hasCriticRating != null)
                    {
                        // Got a result
                        hasCriticRating.CriticRatingSummary = hit.critics_consensus;

                        var rating = float.Parse(hit.ratings.critics_score);

                        if (rating > 0)
                        {
                            hasCriticRating.CriticRating = rating;
                        }
                    }

                    item.SetProviderId(MetadataProviders.RottenTomatoes, hit.id);
                }
            }
        }


        // Utility functions to get the URL of the API calls

        private string GetMovieReviewsUrl(string rtId, string apiKey)
        {
            return BasicUrl + string.Format(MoviesReviews, apiKey, rtId);
        }
        private string GetMovieImdbUrl(string imdbId, string apiKey)
        {
            return BasicUrl + string.Format(MovieImdb, apiKey, imdbId.TrimStart('t'));
        }

        private string GetApiKey()
        {
            var index = Environment.MachineName.GetHashCode() % _apiKeys.Length;

            return _apiKeys[Math.Abs(index)];
        }

        // Data contract classes for use with the Rotten Tomatoes API

        protected class RTReviewList
        {
            public int total { get; set; }
            public List<RTReview> reviews { get; set; }
        }

        protected class RTReview
        {
            public string critic { get; set; }
            public string date { get; set; }
            public string freshness { get; set; }
            public string publication { get; set; }
            public string quote { get; set; }
            public RTReviewLink links { get; set; }
            public string original_score { get; set; }
        }

        protected class RTReviewLink
        {
            public string review { get; set; }
        }

        protected class RTSearchResults
        {
            public int total { get; set; }
            public List<RTMovieSearchResult> movies { get; set; }
            public RTSearchLinks links { get; set; }
            public string link_template { get; set; }
        }

        protected class RTSearchLinks
        {
            public string self { get; set; }
            public string next { get; set; }
            public string previous { get; set; }
        }

        protected class RTMovieSearchResult
        {
            public string title { get; set; }
            public int year { get; set; }
            public string runtime { get; set; }
            public string synopsis { get; set; }
            public string critics_consensus { get; set; }
            public string mpaa_rating { get; set; }
            public string id { get; set; }
            public RTRatings ratings { get; set; }
            public RTAlternateIds alternate_ids { get; set; }
        }

        protected class RTRatings
        {
            public string critics_rating { get; set; }
            public string critics_score { get; set; }
        }

        protected class RTAlternateIds
        {
            public string imdb { get; set; }
        }

    }
}
