using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.RottenTomatoes.Providers
{
    public class RottenTomatoesProvider : IHasItemChangeMonitor
    {
        // http://developer.rottentomatoes.com/iodocs
        private const int DailyRefreshLimit = 300;
        private const string MoviesReviews = @"movies/{1}/reviews.json?review_type=top_critic&page_limit=12&page=1&country=us&apikey={0}";

        private readonly string[] _apiKeys =
            {
                // MB Server key (listed 3x because we have a 30k limit)
                "x9wjnvv39ntjmt9zs95nm7bg",

                // MB Server key (listed 3x because we have a 30k limit)
                "x9wjnvv39ntjmt9zs95nm7bg",

                // MB Server key (listed 3x because we have a 30k limit)
                "x9wjnvv39ntjmt9zs95nm7bg",

                // Donated by Cheesegeezer
                "ag5v28yyk8mna32gujp7q6ze"
            };

        private const string BasicUrl = @"http://api.rottentomatoes.com/api/public/v1.0/";
        private const string MovieImdb = @"movie_alias.json?id={1}&type=imdb&apikey={0}";
        private readonly SemaphoreSlim _rottenTomatoesResourcePool = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _refreshResourcePool = new SemaphoreSlim(1, 1);
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private readonly IItemRepository _itemRepo;
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger _logger;
        private readonly ISecurityManager _security;

        private string RequestHistoryPath
        {
            get
            {
                return Path.Combine(_appPaths.CachePath, "rotten-tomatoes");
            }
        }

        private string RequestHistoryFilePath
        {
            get
            {
                if (!Directory.Exists(RequestHistoryPath))
                {
                    Directory.CreateDirectory(RequestHistoryPath);
                }

                return Path.Combine(RequestHistoryPath, "data.dat");
            }
        }

        public RottenTomatoesProvider(IApplicationPaths appPaths, ILogger logger, IItemRepository itemRepo, IHttpClient httpClient, IJsonSerializer jsonSerializer, ISecurityManager security)
        {
            _appPaths = appPaths;
            _logger = logger;
            _itemRepo = itemRepo;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _security = security;
        }

        /// <summary>
        /// The _configuration
        /// </summary>
        private List<DateTime> _requestHistory;
        /// <summary>
        /// The _configuration initialized
        /// </summary>
        private bool _requestHistoryInitialized;
        /// <summary>
        /// The _configuration sync lock
        /// </summary>
        private object _requestHistorySyncLock = new object();
        /// <summary>
        /// Gets the user's configuration
        /// </summary>
        /// <value>The configuration.</value>
        public List<DateTime> RequestHistory
        {
            get
            {
                // Lazy load
                LazyInitializer.EnsureInitialized(ref _requestHistory, ref _requestHistoryInitialized, ref _requestHistorySyncLock, LoadRequestHistory);
                return _requestHistory;
            }
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

        public Task<ItemUpdateType> FetchAsync(Movie item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchAsync(item, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Trailer item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            if (item.IsLocalTrailer)
            {
                return Task.FromResult(ItemUpdateType.None);
            }

            return FetchAsync(item, cancellationToken);
        }

        public string Name
        {
            get { return "Rotten Tomatoes"; }
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService)
        {
            var imdbId = item.GetProviderId(MetadataProviders.Imdb);

            if (string.IsNullOrEmpty(imdbId))
            {
                return false;
            }

            if ((DateTime.UtcNow - item.DateLastRefreshed).TotalDays > 14)
            {
                if (string.IsNullOrEmpty(item.GetProviderId(RottenTomatoesExternalId.KeyName)))
                {
                    return true;
                }

                if (!_itemRepo.GetCriticReviews(item.Id).Any())
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<ItemUpdateType> FetchAsync(Video item, CancellationToken cancellationToken)
        {
            var existingReviews = _itemRepo.GetCriticReviews(item.Id);
            if (existingReviews.Any())
            {
                return ItemUpdateType.None;
            }

            await _refreshResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            var history = RequestHistory;

            var now = DateTime.UtcNow;

            if (history.Count(i => (now - i).TotalDays <= 1) >= DailyRefreshLimit)
            {
                _refreshResourcePool.Release();

                _logger.Debug("Skipping {0} because daily request limit has been reached.", item.Name);

                return ItemUpdateType.None;
            }

            try
            {
                return await FetchAsyncInternal(item, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                SaveRequestHistory(history);

                _refreshResourcePool.Release();
            }
        }

        private async Task<ItemUpdateType> FetchAsyncInternal(BaseItem item, CancellationToken cancellationToken)
        {
            var imdbId = item.GetProviderId(MetadataProviders.Imdb);

            if (string.IsNullOrEmpty(imdbId))
            {
                return ItemUpdateType.None;
            }

            var apiKey = GetApiKey();

            if (string.IsNullOrEmpty(item.GetProviderId(RottenTomatoesExternalId.KeyName)))
            {
                await FetchRottenTomatoesId(item, apiKey, cancellationToken).ConfigureAwait(false);
            }

            // If still empty we can't continue
            if (string.IsNullOrEmpty(item.GetProviderId(RottenTomatoesExternalId.KeyName)))
            {
                return ItemUpdateType.None;
            }

            if (!_security.IsMBSupporter)
            {
                _logger.Warn("Downloading critic reviews from RottenTomatoes requires a supporter membership.");
                return ItemUpdateType.None;
            }

            RequestHistory.Add(DateTime.UtcNow);

            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = GetMovieReviewsUrl(item.GetProviderId(RottenTomatoesExternalId.KeyName), apiKey),
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

                await _itemRepo.SaveCriticReviews(item.Id, criticReviews).ConfigureAwait(false);
            }

            return ItemUpdateType.MetadataDownload;
        }

        private async Task FetchRottenTomatoesId(BaseItem item, string apiKey, CancellationToken cancellationToken)
        {
            var imdbId = item.GetProviderId(MetadataProviders.Imdb);

            RequestHistory.Add(DateTime.UtcNow);

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

                    item.SetProviderId(RottenTomatoesExternalId.KeyName, hit.id);
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
