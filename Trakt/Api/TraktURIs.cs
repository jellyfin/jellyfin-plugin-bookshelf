namespace Trakt.Api
{
    public static class TraktUris
    {
        public const string Devkey = "0fabacd4bcf52604be7463374d2b9ee91995896ee410bb5ef9ce07ecc18db85c";

        #region POST URI's
        public const string Login = @"https://api-v2launch.trakt.tv/auth/login";

        public const string SyncCollectionAdd = @"https://api-v2launch.trakt.tv/sync/collection";
        public const string SyncCollectionRemove = @"https://api-v2launch.trakt.tv/sync/collection/remove";
        public const string SyncWatchedHistoryAdd = @"https://api-v2launch.trakt.tv/sync/history";
        public const string SyncWatchedHistoryRemove = @"https://api-v2launch.trakt.tv/sync/history/remove";
        public const string SyncRatingsAdd = @"https://api-v2launch.trakt.tv/sync/ratings";

        public const string ScrobbleStart = @"https://api-v2launch.trakt.tv/scrobble/start";
        public const string ScrobblePause = @"https://api-v2launch.trakt.tv/scrobble/pause";
        public const string ScrobbleStop = @"https://api-v2launch.trakt.tv/scrobble/stop";
        #endregion

        #region GET URI's

        public const string WatchedMovies = @"https://api-v2launch.trakt.tv/sync/watched/movies";
        public const string WatchedShows = @"https://api-v2launch.trakt.tv/sync/watched/shows";
        public const string CollectedMovies = @"https://api-v2launch.trakt.tv/sync/collection/movies?extended=metadata";
        public const string CollectedShows = @"https://api-v2launch.trakt.tv/sync/collection/shows?extended=metadata";

        // Recommendations
        public const string RecommendationsMovies = @"https://api-v2launch.trakt.tv/recommendations/movies";
        public const string RecommendationsShows = @"https://api-v2launch.trakt.tv/recommendations/shows";

        #endregion

        #region DELETE 

        // Recommendations
        public const string RecommendationsMoviesDismiss = @"https://api-v2launch.trakt.tv/recommendations/movies/{0}";
        public const string RecommendationsShowsDismiss = @"https://api-v2launch.trakt.tv/recommendations/shows/{0}";

        #endregion
    }
}

