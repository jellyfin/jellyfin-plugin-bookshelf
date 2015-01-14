namespace Trakt.Api
{
    public static class TraktUris
    {
        public const string Devkey = "0fabacd4bcf52604be7463374d2b9ee91995896ee410bb5ef9ce07ecc18db85c";

        #region POST URI's
        public const string Login = @"https://api.trakt.tv/auth/login";

        public const string SyncCollectionAdd = @"https://api.trakt.tv/sync/collection";
        public const string SyncCollectionRemove = @"https://api.trakt.tv/sync/collection/remove";
        public const string SyncWatchedHistoryAdd = @"https://api.trakt.tv/sync/history";
        public const string SyncWatchedHistoryRemove = @"https://api.trakt.tv/sync/history/remove";
        public const string SyncRatingsAdd = @"https://api.trakt.tv/sync/ratings";

        public const string ScrobbleStart = @"https://api.trakt.tv/scrobble/start";
        public const string ScrobblePause = @"https://api.trakt.tv/scrobble/pause";
        public const string ScrobbleStop = @"https://api.trakt.tv/scrobble/stop";
        #endregion

        #region GET URI's

        public const string WatchedMovies = @"https://api.trakt.tv/users/{0}/watched/movies";
        public const string WatchedShows = @"https://api.trakt.tv/users/{0}/watched/shows";
        public const string CollectedMovies = @"https://api.trakt.tv/users/{0}/collection/movies?extended=metadata";
        public const string CollectedShows = @"https://api.trakt.tv/users/{0}/collection/shows?extended=metadata";

        // Recommendations
        public const string RecommendationsMovies = @"https://api.trakt.tv/recommendations/movies";
        public const string RecommendationsShows = @"https://api.trakt.tv/recommendations/shows";

        #endregion

        #region DELETE 

        // Recommendations
        public const string RecommendationsMoviesDismiss = @"https://api.trakt.tv/recommendations/movies/{0}";
        public const string RecommendationsShowsDismiss = @"https://api.trakt.tv/recommendations/shows/{0}";

        #endregion
    }
}

