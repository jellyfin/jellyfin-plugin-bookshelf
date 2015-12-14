using System.Globalization;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using Trakt.Api;
using Trakt.Api.DataContracts.BaseModel;
using Trakt.Api.DataContracts.Users.Collection;
using Trakt.Api.DataContracts.Users.Watched;
using Trakt.Helpers;
using Trakt.Model;

namespace Trakt.ScheduledTasks
{

    /// <summary>
    /// Task that will Sync each users trakt.tv profile with their local library. This task will only include 
    /// watched states.
    /// </summary>
    class SyncFromTraktTask : IScheduledTask
    {
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ILogger _logger;
        private readonly TraktApi _traktApi;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="jsonSerializer"></param>
        /// <param name="userManager"></param>
        /// <param name="userDataManager"> </param>
        /// <param name="httpClient"></param>
        /// <param name="appHost"></param>
        /// <param name="fileSystem"></param>
        public SyncFromTraktTask(ILogManager logger, IJsonSerializer jsonSerializer, IUserManager userManager, IUserDataManager userDataManager, IHttpClient httpClient, IServerApplicationHost appHost, IFileSystem fileSystem)
        {
            _userManager = userManager;
            _userDataManager = userDataManager;
            _logger = logger.GetLogger("Trakt");
            _traktApi = new TraktApi(jsonSerializer, _logger, httpClient, appHost, userDataManager, fileSystem);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var users = _userManager.Users.Where(u => UserHelper.GetTraktUser(u) != null).ToList();

            // No point going further if we don't have users.
            if (users.Count == 0)
            {
                _logger.Info("No Users returned");
                return;
            }

            // purely for progress reporting
            var percentPerUser = 100 / users.Count;
            double currentProgress = 0;
            var numComplete = 0;

            foreach (var user in users)
            {
                try
                {
                    await SyncTraktDataForUser(user, currentProgress, cancellationToken, progress, percentPerUser).ConfigureAwait(false);

                    numComplete++;
                    currentProgress = percentPerUser * numComplete;
                    progress.Report(currentProgress);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error syncing trakt data for user {0}", ex, user.Name);
                }
            }
        }

        private async Task SyncTraktDataForUser(User user, double currentProgress, CancellationToken cancellationToken, IProgress<double> progress, double percentPerUser)
        {
            var libraryRoot = user.RootFolder;
            var traktUser = UserHelper.GetTraktUser(user);

            IEnumerable<TraktMovieWatched> traktWatchedMovies;
            IEnumerable<TraktShowWatched> traktWatchedShows;

            try
            {
                /*
                 * In order to be as accurate as possible. We need to download the users show collection & the users watched shows.
                 * It's unfortunate that trakt.tv doesn't explicitly supply a bulk method to determine shows that have not been watched
                 * like they do for movies.
                 */
                traktWatchedMovies = await _traktApi.SendGetAllWatchedMoviesRequest(traktUser).ConfigureAwait(false);
                traktWatchedShows = await _traktApi.SendGetWatchedShowsRequest(traktUser).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Exception handled", ex);
                throw;
            }


            _logger.Info("Trakt.tv watched Movies count = " + traktWatchedMovies.Count());
            _logger.Info("Trakt.tv watched Shows count = " + traktWatchedShows.Count());


            var mediaItems = libraryRoot.GetRecursiveChildren(user)
                .Where(i => _traktApi.CanSync(i, traktUser))
                .OrderBy(i =>
                {
                    var episode = i as Episode;

                    return episode != null ? episode.Series.Id : i.Id;
                })
                .ToList();

            // purely for progress reporting
            var percentPerItem = percentPerUser / mediaItems.Count;

            foreach (var movie in mediaItems.OfType<Movie>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var matchedMovie = FindMatch(movie, traktWatchedMovies);

                if (matchedMovie != null)
                {
                    _logger.Debug("Movie is in Watched list " + movie.Name);

                    var userData = _userDataManager.GetUserData(user.Id, movie.GetUserDataKey());
                    bool changed = false;

                    // set movie as watched
                    if (!userData.Played)
                    {
                        userData.Played = true;
                        changed = true;
                    }

                    // keep the highest play count
                    int playcount = Math.Max(matchedMovie.Plays, userData.PlayCount);
                    // set movie playcount
                    if (userData.PlayCount != playcount)
                    {
                        userData.PlayCount = playcount;
                        changed = true;
                    }

                    // Set last played to whichever is most recent, remote or local time...
                    if (!string.IsNullOrEmpty(matchedMovie.LastWatchedAt))
                    {
                        var tLastPlayed = DateTime.Parse(matchedMovie.LastWatchedAt);
                        var latestPlayed = tLastPlayed > userData.LastPlayedDate
                            ? tLastPlayed
                            : userData.LastPlayedDate;
                        if (userData.LastPlayedDate != latestPlayed)
                        {
                            userData.LastPlayedDate = latestPlayed;
                            changed = true;
                        }
                    }

                    // Only process if there's a change
                    if (changed)
                    {
                        await
                            _userDataManager.SaveUserData(user.Id, movie, userData, UserDataSaveReason.Import,
                                cancellationToken);
                    }
                }
                else
                {
                    _logger.Info("Failed to match " + movie.Name);
                }

                // purely for progress reporting
                currentProgress += percentPerItem;
                progress.Report(currentProgress);
            }

            foreach (var episode in mediaItems.OfType<Episode>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var matchedShow = FindMatch(episode.Series, traktWatchedShows);

                if (matchedShow != null)
                {
                    var matchedSeason = matchedShow.Seasons
                        .FirstOrDefault(tSeason => tSeason.Number == (episode.ParentIndexNumber == 0? 0 : ((episode.ParentIndexNumber ?? 1) + (episode.Series.AnimeSeriesIndex ?? 1) - 1)));

                    // if it's not a match then it means trakt doesn't know about the season, leave the watched state alone and move on
                    if (matchedSeason != null)
                    {
                        // episode is in users libary. Now we need to determine if it's watched
                        var userData = _userDataManager.GetUserData(user.Id, episode.GetUserDataKey());
                        bool changed = false;

                        var matchedEpisode = matchedSeason.Episodes.FirstOrDefault(x => x.Number == (episode.IndexNumber ?? -1));

                        if (matchedEpisode != null)
                        {
                            _logger.Debug("Episode is in Watched list " + GetVerboseEpisodeData(episode));

                            // Set episode as watched
                            if (!userData.Played)
                            {
                                userData.Played = true;
                                changed = true;
                            }

                            // keep the highest play count
                            int playcount = Math.Max(matchedEpisode.Plays, userData.PlayCount);
                            // set episode playcount
                            if (userData.PlayCount != playcount)
                            {
                                userData.PlayCount = playcount;
                                changed = true;
                            }
                        }
                        else if (!traktUser.SkipUnwatchedImportFromTrakt)
                        {
                            userData.Played = false;
                            userData.PlayCount = 0;
                            userData.LastPlayedDate = null;
                            changed = true;
                        }

                        // only process if changed
                        if (changed)
                        {
                            await
                                _userDataManager.SaveUserData(user.Id, episode, userData, UserDataSaveReason.Import,
                                    cancellationToken);
                        }
                    }
                    else
                    {
                        _logger.Debug("No Season match in Watched shows list " + GetVerboseEpisodeData(episode));
                    }
                }
                else
                {
                    _logger.Debug("No Show match in Watched shows list " + GetVerboseEpisodeData(episode));
                }

                // purely for progress reporting
                currentProgress += percentPerItem;
                progress.Report(currentProgress);
            }
            //_logger.Info(syncItemFailures + " items not parsed");
        }

        private string GetVerboseEpisodeData(Episode episode)
        {
            string episodeString = "";
            episodeString += "Episode: " + (episode.ParentIndexNumber != null ? episode.ParentIndexNumber.ToString() : "null");
            episodeString += "x" + (episode.IndexNumber != null ? episode.IndexNumber.ToString() : "null");
            episodeString += " '" + episode.Name + "' ";
            episodeString += "Series: '" + (episode.Series != null
                ? !String.IsNullOrWhiteSpace(episode.Series.Name)
                    ? episode.Series.Name
                    : "null property"
                : "null class");
            episodeString += "'";

            return episodeString;
        }

        public static TraktShowWatched FindMatch(Series item, IEnumerable<TraktShowWatched> results)
        {
            return results.FirstOrDefault(i => IsMatch(item, i.Show));
        }

        public static TraktShowCollected FindMatch(Series item, IEnumerable<TraktShowCollected> results)
        {
            return results.FirstOrDefault(i => IsMatch(item, i.Show));
        }

        public static TraktMovieWatched FindMatch(BaseItem item, IEnumerable<TraktMovieWatched> results)
        {
            return results.FirstOrDefault(i => IsMatch(item, i.Movie));
        }

        public static IEnumerable<TraktMovieCollected> FindMatches(BaseItem item, IEnumerable<TraktMovieCollected> results)
        {
            return results.Where(i => IsMatch(item, i.Movie)).ToList();
        }

        public static bool IsMatch(BaseItem item, TraktMovie movie)
        {
            var imdb = item.GetProviderId(MetadataProviders.Imdb);

            if (!string.IsNullOrWhiteSpace(imdb) &&
                string.Equals(imdb, movie.Ids.Imdb, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var tmdb = item.GetProviderId(MetadataProviders.Tmdb);

            if (movie.Ids.Tmdb.HasValue && string.Equals(tmdb, movie.Ids.Tmdb.Value.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (item.Name == movie.Title && item.ProductionYear == movie.Year)
            {
                return true;
            }

            return false;
        }

        public static bool IsMatch(Series item, TraktShow show)
        {
                var tvdb = item.GetProviderId(MetadataProviders.Tvdb);
                if (!string.IsNullOrWhiteSpace(tvdb) &&
                    string.Equals(tvdb, show.Ids.Tvdb.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var imdb = item.GetProviderId(MetadataProviders.Imdb);
                if (!string.IsNullOrWhiteSpace(imdb) &&
                    string.Equals(imdb, show.Ids.Imdb, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
        }

        

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new List<ITaskTrigger>();
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get { return "Import playstates from Trakt.tv"; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Description
        {
            get { return "Sync Watched/Unwatched status from Trakt.tv for each MB3 user that has a configured Trakt account"; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Category
        {
            get { return "Trakt"; }
        }
    }
}