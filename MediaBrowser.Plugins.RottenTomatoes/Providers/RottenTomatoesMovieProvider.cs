using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
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
    /// <summary>
    /// Class RottenTomatoesMovieProvider
    /// </summary>
    public class RottenTomatoesProvider : BaseMetadataProvider
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

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        protected IJsonSerializer JsonSerializer { get; private set; }

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        private readonly IItemRepository _itemRepo;

        private readonly string _requestHistoryPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="RottenTomatoesMovieProvider" /> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="appPaths">The app paths.</param>
        public RottenTomatoesProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer, IHttpClient httpClient, IApplicationPaths appPaths, IItemRepository itemRepo)
            : base(logManager, configurationManager)
        {
            JsonSerializer = jsonSerializer;
            HttpClient = httpClient;
            _itemRepo = itemRepo;

            _requestHistoryPath = Path.Combine(appPaths.CachePath, "rotten-tomatoes");
        }

        /// <summary>
        /// Gets the provider version.
        /// </summary>
        /// <value>The provider version.</value>
        protected override string ProviderVersion
        {
            get
            {
                return "7";
            }
        }

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public override bool RequiresInternet
        {
            get
            {
                return false;
            }
        }

        public override ItemUpdateType ItemUpdateType
        {
            get
            {
                return ItemUpdateType.MetadataDownload;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [refresh on version change].
        /// </summary>
        /// <value><c>true</c> if [refresh on version change]; otherwise, <c>false</c>.</value>
        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
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
        /// Gets the request history file path.
        /// </summary>
        /// <value>The request history file path.</value>
        private string RequestHistoryFilePath
        {
            get
            {
                if (!Directory.Exists(_requestHistoryPath))
                {
                    Directory.CreateDirectory(_requestHistoryPath);
                }

                return Path.Combine(_requestHistoryPath, "data.dat");
            }
        }

        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

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

                            if (long.TryParse(i, NumberStyles.Any, UsCulture, out ticks))
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
                        .Select(i => i.Ticks.ToString(UsCulture))
                        .ToArray());

                    streamWriter.Write(text);
                }
            }
        }

        /// <summary>
        /// Supports the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            var trailer = item as Trailer;

            if (trailer != null)
            {
                return !trailer.IsLocalTrailer;
            }

            // Don't support local trailers
            return item is Movie;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get
            {
                // Run after moviedb and xml providers
                return MetadataProviderPriority.Last;
            }
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo info, CancellationToken cancellationToken)
        {
            var existingReviews = _itemRepo.GetCriticReviews(item.Id);
            if (existingReviews.Any())
            {
                SetLastRefreshed(item, DateTime.UtcNow, info);
                return true;
            }

            await _refreshResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            var history = RequestHistory;

            var now = DateTime.UtcNow;

            if (history.Count(i => (now - i).TotalDays <= 1) >= DailyRefreshLimit)
            {
                _refreshResourcePool.Release();

                Logger.Debug("Skipping {0} because daily request limit has been reached. Tomorrow's refresh will retrieve it.", item.Name);

                return false;
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

            return true;
        }

        /// <summary>
        /// Fetches the async internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        private async Task FetchAsyncInternal(BaseItem item, bool force, BaseProviderInfo info, CancellationToken cancellationToken)
        {
            var imdbId = item.GetProviderId(MetadataProviders.Imdb);

            if (string.IsNullOrEmpty(imdbId))
            {
                SetLastRefreshed(item, DateTime.UtcNow, info);
                return;
            }

            var apiKey = GetApiKey();

            if (string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.RottenTomatoes)))
            {
                await FetchRottenTomatoesId(item, apiKey, cancellationToken).ConfigureAwait(false);
            }

            // If still empty we can't continue
            if (string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.RottenTomatoes)))
            {
                SetLastRefreshed(item, DateTime.UtcNow, info);
                return;
            }

            RequestHistory.Add(DateTime.UtcNow);

            using (var stream = await HttpClient.Get(new HttpRequestOptions
            {
                Url = GetMovieReviewsUrl(item.GetProviderId(MetadataProviders.RottenTomatoes), apiKey),
                ResourcePool = _rottenTomatoesResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {

                var result = JsonSerializer.DeserializeFromStream<RTReviewList>(stream);

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

            SetLastRefreshed(item, DateTime.UtcNow, info);
        }

        /// <summary>
        /// Fetches the rotten tomatoes id.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchRottenTomatoesId(BaseItem item, string apiKey, CancellationToken cancellationToken)
        {
            var imdbId = item.GetProviderId(MetadataProviders.Imdb);

            RequestHistory.Add(DateTime.UtcNow);

            // Have IMDB Id
            using (var stream = await HttpClient.Get(new HttpRequestOptions
            {
                Url = GetMovieImdbUrl(imdbId, apiKey),
                ResourcePool = _rottenTomatoesResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                var hit = JsonSerializer.DeserializeFromStream<RTMovieSearchResult>(stream);

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