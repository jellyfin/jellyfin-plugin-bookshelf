namespace Trakt.ScheduledTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using CommonIO;

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

    using Trakt.Api;
    using Trakt.Api.DataContracts.Sync;
    using Trakt.Helpers;
    using Trakt.Model;

    /// <summary>
    /// Task that will Sync each users local library with their respective trakt.tv profiles. This task will only include 
    /// titles, watched states will be synced in other tasks.
    /// </summary>
    public class SyncLibraryTask : IScheduledTask
    {
        //private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;
        private readonly TraktApi _traktApi;
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;

        public SyncLibraryTask(ILogManager logger, IJsonSerializer jsonSerializer, IUserManager userManager, IUserDataManager userDataManager, IHttpClient httpClient, IServerApplicationHost appHost, IFileSystem fileSystem, ILibraryManager libraryManager)
        {
            _jsonSerializer = jsonSerializer;
            _userManager = userManager;
            _userDataManager = userDataManager;
            _libraryManager = libraryManager;
            _logger = logger.GetLogger("Trakt");
            _traktApi = new TraktApi(jsonSerializer, _logger, httpClient, appHost, userDataManager, fileSystem);
        }

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new List<ITaskTrigger>();
        }

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
            var progPercent = 0.0;
            var percentPerUser = 100 / users.Count;

            foreach (var user in users)
            {
                var traktUser = UserHelper.GetTraktUser(user);

                // I'll leave this in here for now, but in reality this continue should never be reached.
                if (string.IsNullOrEmpty(traktUser?.LinkedMbUserId))
                {
                    _logger.Error("traktUser is either null or has no linked MB account");
                    continue;
                }

                await SyncUserLibrary(user, traktUser, progPercent, percentPerUser, progress, cancellationToken)
                        .ConfigureAwait(false);

                progPercent += percentPerUser;
            }
        }

        private async Task SyncUserLibrary(
            User user,
            TraktUser traktUser,
            double progPercent,
            double percentPerUser,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            // purely for progress reporting
            var mediaItemsCount = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Movie).Name, typeof(Episode).Name },
                ExcludeLocationTypes = new[] { LocationType.Virtual }
            })
            .Count(i => _traktApi.CanSync(i, traktUser));

            if (mediaItemsCount == 0)
            {
                _logger.Info("No media found for '" + user.Name + "'.");
                return;
            }

            _logger.Info(mediaItemsCount + " Items found for '" + user.Name + "'.");

            var percentPerItem = (float)percentPerUser / mediaItemsCount / 2.0;

            await SyncMovies(user, traktUser, progress, progPercent, percentPerItem, cancellationToken);
            await SyncShows(user, traktUser, progress, progPercent, percentPerItem, cancellationToken);
        }

        /// <summary>
        /// Sync watched and collected status of <see cref="Movie"/>s with trakt.
        /// </summary>
        /// <param name="user">
        /// <see cref="User"/> to get <see cref="UserItemData"/> (e.g. watched status) from.
        /// </param>
        /// <param name="traktUser">
        /// The <see cref="TraktUser"/> to sync with.
        /// </param>
        /// <param name="progress">
        /// Progress reporter.
        /// </param>
        /// <param name="progPercent">
        /// Initial progress value.
        /// </param>
        /// <param name="percentPerItem">
        /// Progress percent per item.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// Awaitable <see cref="Task"/>.
        /// </returns>
        private async Task SyncMovies(
            User user,
            TraktUser traktUser,
            IProgress<double> progress,
            double progPercent,
            double percentPerItem,
            CancellationToken cancellationToken)
        {
            /*
             * In order to sync watched status to trakt.tv we need to know what's been watched on Trakt already. This
             * will stop us from endlessly incrementing the watched values on the site.
             */
            var traktWatchedMovies = await _traktApi.SendGetAllWatchedMoviesRequest(traktUser).ConfigureAwait(false);
            var traktCollectedMovies = await _traktApi.SendGetAllCollectedMoviesRequest(traktUser).ConfigureAwait(false);
            var libraryMovies =
                _libraryManager.GetItemList(
                        new InternalItemsQuery
                            {
                                IncludeItemTypes = new[] { typeof(Movie).Name },
                                ExcludeLocationTypes = new[] { LocationType.Virtual }
                            })
                    .Where(x => _traktApi.CanSync(x, traktUser))
                    .OrderBy(x => x.Name)
                    .ToList();
            var collectedMovies = new List<Movie>();
            var playedMovies = new List<Movie>();
            var unplayedMovies = new List<Movie>();

            foreach (var child in libraryMovies)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var libraryMovie = child as Movie;
                var userData = _userDataManager.GetUserData(user.Id, child);
                
                // if movie is not collected, or (export media info setting is enabled and every collected matching movie has different metadata), collect it
                var collectedMathingMovies = SyncFromTraktTask.FindMatches(libraryMovie, traktCollectedMovies).ToList();
                if (!collectedMathingMovies.Any()
                    || (traktUser.ExportMediaInfo
                        && collectedMathingMovies.All(collectedMovie => collectedMovie.MetadataIsDifferent(libraryMovie))))
                {
                    collectedMovies.Add(libraryMovie);
                }

                var movieWatched = SyncFromTraktTask.FindMatch(libraryMovie, traktWatchedMovies);

                // if the movie has been played locally and is unplayed on trakt.tv then add it to the list
                if (userData.Played)
                {
                    if (movieWatched == null)
                    {
                        if (traktUser.PostWatchedHistory)
                        {
                            playedMovies.Add(libraryMovie);
                        }
                        else
                        {
                            userData.Played = false;
                            await
                                _userDataManager.SaveUserData(
                                    user.Id,
                                    libraryMovie,
                                    userData,
                                    UserDataSaveReason.Import,
                                    cancellationToken);
                        }
                    }
                }
                else
                {
                    // If the show has not been played locally but is played on trakt.tv then add it to the unplayed list
                    if (movieWatched != null)
                    {
                        unplayedMovies.Add(libraryMovie);
                    }
                }

                // purely for progress reporting
                progPercent += percentPerItem;
                progress.Report(progPercent);
            }

            // send movies to mark collected
            await SendMovieCollectionUpdates(true, traktUser, collectedMovies, progress, progPercent, percentPerItem, cancellationToken);

            // send movies to mark watched
            await SendMoviePlaystateUpdates(true, traktUser, playedMovies, progress, progPercent, percentPerItem, cancellationToken);

            // send movies to mark unwatched
            await SendMoviePlaystateUpdates(false, traktUser, unplayedMovies, progress, progPercent, percentPerItem, cancellationToken);
        }

        private async Task SendMovieCollectionUpdates(
            bool collected,
            TraktUser traktUser,
            List<Movie> movies,
            IProgress<double> progress,
            double progPercent,
            double percentPerItem,
            CancellationToken cancellationToken)
        {
            _logger.Info("Movies to " + (collected ? "add to" : "remove from") + " Collection: " + movies.Count);
            if (movies.Count > 0)
            {
                try
                {
                    var dataContracts =
                        await
                            _traktApi.SendLibraryUpdateAsync(movies, traktUser, cancellationToken, collected ? EventType.Add : EventType.Remove)
                                .ConfigureAwait(false);
                    if (dataContracts != null)
                    {
                        foreach (var traktSyncResponse in dataContracts)
                        {
                            LogTraktResponseDataContract(traktSyncResponse);
                        }
                    }
                }
                catch (ArgumentNullException argNullEx)
                {
                    _logger.ErrorException("ArgumentNullException handled sending movies to trakt.tv", argNullEx);
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Exception handled sending movies to trakt.tv", e);
                }

                // purely for progress reporting
                progPercent += percentPerItem * movies.Count;
                progress.Report(progPercent);
            }
        }

        private async Task SendMoviePlaystateUpdates(
            bool seen,
            TraktUser traktUser,
            List<Movie> playedMovies,
            IProgress<double> progress,
            double progPercent,
            double percentPerItem,
            CancellationToken cancellationToken)
        {
            _logger.Info("Movies to set " + (seen ? string.Empty : "un") + "watched: " + playedMovies.Count);
            if (playedMovies.Count > 0)
            {
                try
                {
                    var dataContracts =
                        await _traktApi.SendMoviePlaystateUpdates(playedMovies, traktUser, seen, cancellationToken);
                    if (dataContracts != null)
                    {
                        foreach (var traktSyncResponse in dataContracts)
                        {
                            LogTraktResponseDataContract(traktSyncResponse);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Error updating movie play states", e);
                }

                // purely for progress reporting
                progPercent += percentPerItem * playedMovies.Count;
                progress.Report(progPercent);
            }
        }

        private async Task SyncShows(
            User user,
            TraktUser traktUser,
            IProgress<double> progress,
            double progPercent,
            double percentPerItem,
            CancellationToken cancellationToken)
        {
            var traktWatchedShows = await _traktApi.SendGetWatchedShowsRequest(traktUser).ConfigureAwait(false);
            var traktCollectedShows = await _traktApi.SendGetCollectedShowsRequest(traktUser).ConfigureAwait(false);
            var episodeItems =
                _libraryManager.GetItemList(
                        new InternalItemsQuery
                            {
                                IncludeItemTypes = new[] { typeof(Episode).Name },
                                ExcludeLocationTypes = new[] { LocationType.Virtual }
                            })
                    .Where(x => _traktApi.CanSync(x, traktUser))
                    .OrderBy(x => (x as Episode)?.SeriesName)
                    .ToList();

            var collectedEpisodes = new List<Episode>();
            var playedEpisodes = new List<Episode>();
            var unplayedEpisodes = new List<Episode>();

            foreach (var child in episodeItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var episode = child as Episode;
                var userData = _userDataManager.GetUserData(user.Id, episode);
                var isPlayedTraktTv = false;
                var traktWatchedShow = SyncFromTraktTask.FindMatch(episode.Series, traktWatchedShows);

                if (traktWatchedShow?.Seasons != null && traktWatchedShow.Seasons.Count > 0)
                {
                    isPlayedTraktTv =
                        traktWatchedShow.Seasons.Any(
                            season =>
                                season.Number == episode.GetSeasonNumber() && season.Episodes != null
                                && season.Episodes.Any(te => te.Number == episode.IndexNumber && te.Plays > 0));
                }

                // if the show has been played locally and is unplayed on trakt.tv then add it to the list
                if (userData != null && userData.Played && !isPlayedTraktTv)
                {
                    if (traktUser.PostWatchedHistory)
                    {
                        playedEpisodes.Add(episode);
                    }
                    else
                    {
                        userData.Played = false;
                        await
                            _userDataManager.SaveUserData(
                                user.Id,
                                episode,
                                userData,
                                UserDataSaveReason.Import,
                                cancellationToken);
                    }
                }
                else if (userData != null && !userData.Played && isPlayedTraktTv)
                {
                    // If the show has not been played locally but is played on trakt.tv then add it to the unplayed list
                    unplayedEpisodes.Add(episode);
                }

                var traktCollectedShow = SyncFromTraktTask.FindMatch(episode.Series, traktCollectedShows);
                if (traktCollectedShow?.Seasons == null
                    || traktCollectedShow.Seasons.All(x => x.Number != episode.ParentIndexNumber)
                    || traktCollectedShow.Seasons.First(x => x.Number == episode.ParentIndexNumber)
                        .Episodes.All(e => e.Number != episode.IndexNumber))
                {
                    collectedEpisodes.Add(episode);
                }

                // purely for progress reporting
                progPercent += percentPerItem;
                progress.Report(progPercent);
            }

            await SendEpisodeCollectionUpdates(true, traktUser, collectedEpisodes, progress, progPercent, percentPerItem, cancellationToken);

            await SendEpisodePlaystateUpdates(true, traktUser, playedEpisodes, progress, progPercent, percentPerItem, cancellationToken);

            await SendEpisodePlaystateUpdates(false, traktUser, unplayedEpisodes, progress, progPercent, percentPerItem, cancellationToken);
        }

        private async Task SendEpisodePlaystateUpdates(
            bool seen,
            TraktUser traktUser,
            List<Episode> playedEpisodes,
            IProgress<double> progress,
            double progPercent,
            double percentPerItem,
            CancellationToken cancellationToken)
        {
            _logger.Info("Episodes to set " + (seen ? string.Empty : "un") + "watched: " + playedEpisodes.Count);
            if (playedEpisodes.Count > 0)
            {
                try
                {
                    var dataContracts =
                        await _traktApi.SendEpisodePlaystateUpdates(playedEpisodes, traktUser, seen, cancellationToken);
                    dataContracts?.ForEach(LogTraktResponseDataContract);
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Error updating episode play states", e);
                }

                // purely for progress reporting
                progPercent += percentPerItem * playedEpisodes.Count;
                progress.Report(progPercent);
            }
        }

        private async Task SendEpisodeCollectionUpdates(
            bool collected,
            TraktUser traktUser,
            List<Episode> collectedEpisodes,
            IProgress<double> progress,
            double progPercent,
            double percentPerItem,
            CancellationToken cancellationToken)
        {
            _logger.Info("Episodes to add to Collection: " + collectedEpisodes.Count);
            if (collectedEpisodes.Count > 0)
            {
                try
                {
                    var dataContracts =
                        await
                            _traktApi.SendLibraryUpdateAsync(collectedEpisodes, traktUser, cancellationToken, collected ? EventType.Add : EventType.Remove)
                                .ConfigureAwait(false);
                    if (dataContracts != null)
                    {
                        foreach (var traktSyncResponse in dataContracts)
                        {
                            LogTraktResponseDataContract(traktSyncResponse);
                        }
                    }
                }
                catch (ArgumentNullException argNullEx)
                {
                    _logger.ErrorException("ArgumentNullException handled sending episodes to trakt.tv", argNullEx);
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Exception handled sending episodes to trakt.tv", e);
                }

                // purely for progress reporting
                progPercent += percentPerItem * collectedEpisodes.Count;
                progress.Report(progPercent);
            }
        }

        public string Name => "Sync library to trakt.tv";

        public string Category => "Trakt";

        public string Description => "Adds any media that is in each users trakt monitored locations to their trakt.tv profile";

        private void LogTraktResponseDataContract(TraktSyncResponse dataContract)
        {
            _logger.Debug("TraktResponse Added Movies: " + dataContract.Added.Movies);
            _logger.Debug("TraktResponse Added Shows: " + dataContract.Added.Shows);
            _logger.Debug("TraktResponse Added Seasons: " + dataContract.Added.Seasons);
            _logger.Debug("TraktResponse Added Episodes: " + dataContract.Added.Episodes);
            foreach (var traktMovie in dataContract.NotFound.Movies)
            {
                _logger.Error("TraktResponse not Found:" + _jsonSerializer.SerializeToString(traktMovie));
            }

            foreach (var traktShow in dataContract.NotFound.Shows)
            {
                _logger.Error("TraktResponse not Found:" + _jsonSerializer.SerializeToString(traktShow));
            }

            foreach (var traktSeason in dataContract.NotFound.Seasons)
            {
                _logger.Error("TraktResponse not Found:" + _jsonSerializer.SerializeToString(traktSeason));
            }

            foreach (var traktEpisode in dataContract.NotFound.Episodes)
            {
                _logger.Error("TraktResponse not Found:" + _jsonSerializer.SerializeToString(traktEpisode));
            }
        }
    }
}
