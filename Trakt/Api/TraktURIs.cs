namespace Trakt.Api
{
    public static class TraktUris
    {
        #region POST URI's

        private const string Devkey = "77449c38c9f4f9b3cdb8d3f9ef0566b167a935eb";

        // Account
        public const string AccountTest = @"http://api.trakt.tv/account/test/" + Devkey;
        public const string AccountSettings = @"http://api.trakt.tv/account/settings/" + Devkey;
        // Friends
        public const string FriendsAdd = @"http://api.trakt.tv/friends/add/" + Devkey;
        public const string FriendsAll = @"http://api.trakt.tv/friends/all/" + Devkey;
        public const string FriendsApprove = @"http://api.trakt.tv/friends/approve/" + Devkey;
        public const string FriendsDelete = @"http://api.trakt.tv/friends/delete/" + Devkey;
        public const string FriendsDeny = @"http://api.trakt.tv/friends/deny/" + Devkey;
        public const string FriendsRequests = @"http://api.trakt.tv/friends/requests/" + Devkey;
        // Movie
        public const string MovieCancelWatching = @"http://api.trakt.tv/movie/cancelwatching/" + Devkey;
        public const string MovieScrobble = @"http://api.trakt.tv/movie/scrobble/" + Devkey;
        public const string MovieSeen = @"http://api.trakt.tv/movie/seen/" + Devkey;
        public const string MovieLibrary = @"http://api.trakt.tv/movie/library/" + Devkey;
        public const string MovieUnLibrary = @"http://api.trakt.tv/movie/unlibrary/" + Devkey;
        public const string MovieUnSeen = @"http://api.trakt.tv/movie/unseen/" + Devkey;
        public const string MovieUnWatchList = @"http://api.trakt.tv/movie/unwatchlist/" + Devkey;
        public const string MovieWatching = @"http://api.trakt.tv/movie/watching/" + Devkey;
        public const string MovieWatchList = @"http://api.trakt.tv/movie/watchlist/" + Devkey;

        // Rate
        public const string RateEpisode = @"http://api.trakt.tv/rate/episode/" + Devkey;
        public const string RateMovie = @"http://api.trakt.tv/rate/movie/" + Devkey;
        public const string RateShow = @"http://api.trakt.tv/rate/show/" + Devkey;

        // Recommendations
        public const string RecommendationsMovies = @"http://api.trakt.tv/recommendations/movies/" + Devkey;
        public const string RecommendationsShows = @"http://api.trakt.tv/recommendations/shows/" + Devkey;
        public const string RecommendationsMoviesDismiss = @"http://api.trakt.tv/recommendations/movies/dismiss/" + Devkey;
        public const string RecommendationsShowsDismiss = @"http://api.trakt.tv/recommendations/shows/dismiss/" + Devkey;

        // Comment
        public const string CommentEpisode = @"http://api.trakt.tv/comment/episode/" + Devkey;
        public const string CommentMovie = @"http://api.trakt.tv/comment/movie/" + Devkey;
        public const string CommentShow = @"http://api.trakt.tv/comment/show/" + Devkey;

        // Show
        public const string ShowCancelWatching = @"http://api.trakt.tv/show/cancelwatching/" + Devkey;
        public const string ShowEpisodeLibrary = @"http://api.trakt.tv/show/episode/library/" + Devkey;
        public const string ShowEpisodeSeen = @"http://api.trakt.tv/show/episode/seen/" + Devkey;
        public const string ShowEpisodeUnLibrary = @"http://api.trakt.tv/show/episode/unlibrary/" + Devkey;
        public const string ShowEpisodeUnSeen = @"http://api.trakt.tv/show/episode/unseen/" + Devkey;
        public const string ShowEpisodeUnWatchList = @"http://api.trakt.tv/show/episode/unwatchlist/" + Devkey;
        public const string ShowEpisodeWatchList = @"http://api.trakt.tv/show/episode/watchlist/" + Devkey;
        public const string ShowScrobble = @"http://api.trakt.tv/show/scrobble/" + Devkey;
        public const string ShowUnLibrary = @"http://api.trakt.tv/show/unlibrary/" + Devkey;
        public const string ShowUnWatchList = @"http://api.trakt.tv/show/unwatchlist/" + Devkey;
        public const string ShowWatching = @"http://api.trakt.tv/show/watching/" + Devkey;
        public const string ShowWatchList = @"http://api.trakt.tv/show/watchlist/" + Devkey;

        #endregion

        #region GET URI's

        // Movie
        public const string MovieSummary = @"http://api.trakt.tv/movie/summary.json/" + Devkey + @"/{0}";
        public const string MoviesTrending = @"http://api.trakt.tv/movies/trending.json/" + Devkey;
        public const string MovieShouts = @"http://api.trakt.tv/movie/shouts.json/" + Devkey + @"/{0}";

        // Show
        public const string EpisodeSummary = @"http://api.trakt.tv/show/episode/summary.json/" + Devkey + @"/{0}/{1}/{2}";
        public const string EpisodeShouts = @"http://api.trakt.tv/show/episode/shouts.json/" + Devkey + @"/{0}/{1}/{2}";
        public const string ShowsTrending = @"http://api.trakt.tv/shows/trending.json/" + Devkey;
        public const string ShowShouts = @"http://api.trakt.tv/show/shouts.json/" + Devkey + @"/{0}";
        public const string ShowSummary = @"http://api.trakt.tv/show/summary.json/" + Devkey + @"/{0}";

        // User
        public const string MoviesAll = @"http://api.trakt.tv/user/library/movies/all.json/" + Devkey + @"/{0}";
        public const string ShowsCollection = @"http://api.trakt.tv/user/library/shows/collection.json/" + Devkey + @"/{0}";
        public const string ShowsWatched = @"http://api.trakt.tv/user/library/shows/watched.json/" + Devkey + @"/{0}";
        public const string WatchedEpisodes = @"http://api.trakt.tv/user/watched/episodes.json/" + Devkey + @"/{0}";
        public const string WatchedMovies = @"http://api.trakt.tv/user/watched/movies.json/" + Devkey + @"/{0}";
        public const string UserLists = @"http://api.trakt.tv/user/lists.json/" + Devkey + @"/{0}";
        public const string List = @"http://api.trakt.tv/user/list.json/" + Devkey + @"/{0}/{1}";
        public const string WatchlistMovies = @"http://api.trakt.tv/user/watchlist/movies.json/" + Devkey + @"/{0}";
        public const string WatchlistShows = @"http://api.trakt.tv/user/watchlist/shows.json/" + Devkey + @"/{0}";
        public const string WatchlistEpisodes = @"http://api.trakt.tv/user/watchlist/episodes.json/" + Devkey + @"/{0}";
        public const string UserProfile = @"http://api.trakt.tv/user/profile.json/" + Devkey + @"/{0}";
        public const string Friends = @"http://api.trakt.tv/user/network/friends.json/" + Devkey + @"/{0}";

        // Activity
        public const string ActivityUser = @"http://api.trakt.tv/activity/user.json/" + Devkey + @"/{0}/{1}/{2}";
        //public const string ActivityFriends = @"http://api.trakt.tv/activity/friends.json/" + DEVKEY + @"/{0}/{1}/{2}/{3}";
        public const string ActivityFriends = @"http://api.trakt.tv/activity/friends.json/" + Devkey + @"/{0}/{1}";

        //Calendar
        public const string UserCalendar = @"http://api.trakt.tv/user/calendar/shows.json/" + Devkey + @"/{0}";
        public const string PremieresCalendar = @"http://api.trakt.tv/calendar/premieres.json/" + Devkey;

        #endregion
    }
}

