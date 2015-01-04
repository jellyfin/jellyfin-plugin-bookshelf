//using MediaBrowser.Common.IO;
//using MediaBrowser.Common.Net;
//using MediaBrowser.Common.ScheduledTasks;
//using MediaBrowser.Controller.Entities;
//using MediaBrowser.Controller.Entities.Movies;
//using MediaBrowser.Controller.Entities.TV;
//using MediaBrowser.Controller.Library;
//using MediaBrowser.Model.Entities;
//using MediaBrowser.Model.Logging;
//using MediaBrowser.Model.Serialization;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Trakt.Api;
//using Trakt.Api.DataContracts;
//using Trakt.Helpers;
//using Trakt.Model;
//
//namespace Trakt.ScheduledTasks
//{
//    /// <summary>
//    /// Task that will Sync each users local library with their respective trakt.tv profiles. This task will only include 
//    /// titles, watched states will be synced in other tasks.
//    /// </summary>
//    public class SyncLibraryTask : IScheduledTask
//    {
//        //private readonly IHttpClient _httpClient;
//        private readonly IUserManager _userManager;
//        private readonly ILogger _logger;
//        private readonly IFileSystem _fileSystem;
//        private readonly TraktApi _traktApi;
//        private readonly IUserDataManager _userDataManager;
//
//        public SyncLibraryTask(ILogManager logger, IJsonSerializer jsonSerializer, IUserManager userManager, IUserDataManager userDataManager, IHttpClient httpClient, IFileSystem fileSystem)
//        {
//            _userManager = userManager;
//            _userDataManager = userDataManager;
//            _logger = logger.GetLogger("Trakt");
//            _fileSystem = fileSystem;
//            _traktApi = new TraktApi(jsonSerializer, _logger, httpClient);
//        }
//
//        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
//        {
//            return new List<ITaskTrigger>();
//        }
//
//        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
//        {
//            var users = _userManager.Users.Where(u =>
//            {
//                var traktUser = UserHelper.GetTraktUser(u);
//
//                return traktUser != null;
//
//            }).ToList();
//
//            // No point going further if we don't have users.
//            if (users.Count == 0)
//            {
//                _logger.Info("No Users returned");
//                return;
//            }
//
//            // purely for progress reporting
//            var progPercent = 0.0;
//            var percentPerUser = 100 / users.Count;
//
//            foreach (var user in users)
//            {
//                var traktUser = UserHelper.GetTraktUser(user);
//
//                // I'll leave this in here for now, but in reality this continue should never be reached.
//                if (traktUser == null || String.IsNullOrEmpty(traktUser.LinkedMbUserId))
//                {
//                    _logger.Error("traktUser is either null or has no linked MB account");
//                    continue;
//                }
//
//                await SyncUserLibrary(user, traktUser, progPercent, percentPerUser, progress, cancellationToken)
//                        .ConfigureAwait(false);
//
//                progPercent += percentPerUser;
//            }
//        }
//
//        private async Task SyncUserLibrary(User user, 
//            TraktUser traktUser, 
//            double progPercent,
//            double percentPerUser, 
//            IProgress<double> progress,
//            CancellationToken cancellationToken)
//        {
//            var libraryRoot = user.RootFolder;
//
//            /*
//             * In order to sync watched status to trakt.tv we need to know what's been watched on Trakt already. This
//             * will stop us from endlessly incrementing the watched values on the site.
//             */
//            List<TraktMovieDataContract> tMovies = await _traktApi.SendGetAllMoviesRequest(traktUser).ConfigureAwait(false);
//            List<TraktUserLibraryShowDataContract> tShowsWatched = await _traktApi.SendGetWatchedShowsRequest(traktUser).ConfigureAwait(false);
//
//            var movies = new List<Movie>();
//            var episodes = new List<Episode>();
//            var playedMovies = new List<Movie>();
//            var playedEpisodes = new List<Episode>();
//            var unPlayedMovies = new List<Movie>();
//            var unPlayedEpisodes = new List<Episode>();
//            var currentSeriesId = Guid.Empty;
//
//            var mediaItems = libraryRoot.GetRecursiveChildren(user)
//                .Where(SyncFromTraktTask.CanSync)
//                .ToList();
//
//            if (mediaItems.Count == 0)
//            {
//                _logger.Info("No trakt media found for '" + user.Name + "'. Have trakt locations been configured?");
//                return;
//            }
//
//            // purely for progress reporting
//            var percentPerItem = percentPerUser / mediaItems.Count;
//
//            foreach (var child in mediaItems)
//            {
//                cancellationToken.ThrowIfCancellationRequested();
//
//                if (child.Path == null || child.LocationType == LocationType.Virtual) continue;
//
//                if (child is Movie)
//                {
//                    var movie = (Movie) child;
//                    movies.Add(movie);
//
//                    var userData = _userDataManager.GetUserData(user.Id, child.GetUserDataKey());
//
//                    var traktTvMovie = SyncFromTraktTask.FindMatch(child, tMovies);
//
//                    if (traktTvMovie != null && userData.Played && (traktTvMovie.Watched == false || traktTvMovie.Plays < userData.PlayCount))
//                    {
//                        playedMovies.Add(movie);
//                    }
//                    else if (traktTvMovie != null && traktTvMovie.Watched && !userData.Played)
//                    {
//                        unPlayedMovies.Add(movie);
//                    }
//
//                    // publish if the list hits a certain size
//                    if (movies.Count >= 120)
//                    {
//                        // Add movies to library
//                        try
//                        {
//                            var dataContract = await _traktApi.SendLibraryUpdateAsync(movies, traktUser, cancellationToken, EventType.Add).ConfigureAwait(false);
//                            if (dataContract != null)
//                                LogTraktResponseDataContract(dataContract);
//                        }
//                        catch (ArgumentNullException argNullEx)
//                        {
//                            _logger.ErrorException("ArgumentNullException handled sending movies to trakt.tv", argNullEx);
//                        }
//                        catch (Exception e)
//                        {
//                            _logger.ErrorException("Exception handled sending movies to trakt.tv", e);
//                        }
//                        movies.Clear();
//
//                        // Mark movies seen
//                        if (playedMovies.Count > 0)
//                        {
//                            try
//                            {
//                                var dataCotract = await _traktApi.SendMoviePlaystateUpdates(playedMovies, traktUser, true, cancellationToken);
//                                if (dataCotract != null)
//                                    LogTraktResponseDataContract(dataCotract);
//                            }
//                            catch (Exception e)
//                            {
//                                _logger.ErrorException("Error updating played state", e);
//                            }
//
//                            playedMovies.Clear();
//                        }
//
//                        // Mark movies unseen
//                        if (unPlayedMovies.Count > 0)
//                        {
//                            try
//                            {
//                                var dataContract = await _traktApi.SendMoviePlaystateUpdates(unPlayedMovies, traktUser, false, cancellationToken);
//                                if (dataContract != null)
//                                    LogTraktResponseDataContract(dataContract);
//                            }
//                            catch (Exception e)
//                            {
//                                _logger.ErrorException("Error updating played state", e);
//                            }
//
//                            unPlayedMovies.Clear();
//                        }
//                    }
//                }
//                else if (child is Episode)
//                {
//                    var ep = child as Episode;
//                    var series = ep.Series;
//
//                    var userData = _userDataManager.GetUserData(user.Id, ep.GetUserDataKey());
//
//                    var isPlayedTraktTv = false;
//
//                    var traktTvShow = SyncFromTraktTask.FindMatch(series, tShowsWatched);
//
//                    if (traktTvShow != null && traktTvShow.Seasons != null && traktTvShow.Seasons.Count > 0)
//                    {
//                        foreach (var episode in from season in traktTvShow.Seasons where ep.Season != null && season.Season.Equals(ep.Season.IndexNumber) && season.Episodes != null && season.Episodes.Count > 0 from episode in season.Episodes where episode.Equals(ep.IndexNumber) select episode)
//                        {
//                            isPlayedTraktTv = true;
//                        }
//                    }
//
//                    if (series != null && currentSeriesId != series.Id && episodes.Count > 0)
//                    {
//                        // We're starting a new show. Finish up with the old one
//
//                        // Add episodes to trakt.tv library
//                        try
//                        {
//                            var dataContract = await _traktApi.SendLibraryUpdateAsync(episodes, traktUser, cancellationToken, EventType.Add).ConfigureAwait(false);
//                            if (dataContract != null)
//                                LogTraktResponseDataContract(dataContract);
//                        }
//                        catch (ArgumentNullException argNullEx)
//                        {
//                            _logger.ErrorException("ArgumentNullException handled sending episodes to trakt.tv", argNullEx);
//                        }
//                        catch (Exception e)
//                        {
//                            _logger.ErrorException("Exception handled sending episodes to trakt.tv", e);
//                        }
//
//                        // Update played state of these episodes
//                        if (playedEpisodes.Count > 0)
//                        {
//                            try
//                            {
//                                var dataContracts = await _traktApi.SendEpisodePlaystateUpdates(playedEpisodes, traktUser, true, cancellationToken);
//                                if (dataContracts != null)
//                                {
//                                    foreach (var dataContract in dataContracts)
//                                        LogTraktResponseDataContract(dataContract);
//                                }
//                            }
//                            catch (Exception e)
//                            {
//                                _logger.ErrorException("Exception handled sending played episodes to trakt.tv", e);
//                            }
//                        }
//
//                        if (unPlayedEpisodes.Count > 0)
//                        {
//                            try
//                            {
//                                var dataContracts = await _traktApi.SendEpisodePlaystateUpdates(unPlayedEpisodes, traktUser, false, cancellationToken);
//                                if (dataContracts != null)
//                                {
//                                    foreach (var dataContract in dataContracts)
//                                        LogTraktResponseDataContract(dataContract);
//                                }
//                            }
//                            catch (Exception e)
//                            {
//                                _logger.ErrorException("Exception handled sending played episodes to trakt.tv", e);
//                            }
//                        }
//
//                        episodes.Clear();
//                        playedEpisodes.Clear();
//                        unPlayedEpisodes.Clear();
//                    }
//
//                    if (ep.Series != null)
//                    {
//                        currentSeriesId = ep.Series.Id;
//                        episodes.Add(ep);
//                    }
//
//                    // if the show has been played locally and is unplayed on trakt.tv then add it to the list
//                    if (userData != null && userData.Played && !isPlayedTraktTv)
//                    {
//                        playedEpisodes.Add(ep);
//                    }
//                    // If the show has not been played locally but is played on trakt.tv then add it to the unplayed list
//                    else if (userData != null && !userData.Played && isPlayedTraktTv)
//                    {
//                        unPlayedEpisodes.Add(ep);
//                    }
//                }
//
//                // purely for progress reporting
//                progPercent += percentPerItem;
//                progress.Report(progPercent);
//            }
//
//            // send any remaining entries
//            if (movies.Count > 0)
//            {
//                try
//                {
//                    var dataContract = await _traktApi.SendLibraryUpdateAsync(movies, traktUser, cancellationToken, EventType.Add).ConfigureAwait(false);
//                    if (dataContract != null)
//                        LogTraktResponseDataContract(dataContract);
//                }
//                catch (ArgumentNullException argNullEx)
//                {
//                    _logger.ErrorException("ArgumentNullException handled sending movies to trakt.tv", argNullEx);
//                }
//                catch (Exception e)
//                {
//                    _logger.ErrorException("Exception handled sending movies to trakt.tv", e);
//                }
//
//            }
//
//            if (episodes.Count > 0)
//            {
//                try
//                {
//                    var dataContract = await _traktApi.SendLibraryUpdateAsync(episodes, traktUser, cancellationToken, EventType.Add).ConfigureAwait(false);
//                    if (dataContract != null)
//                        LogTraktResponseDataContract(dataContract);
//                }
//                catch (ArgumentNullException argNullEx)
//                {
//                    _logger.ErrorException("ArgumentNullException handled sending episodes to trakt.tv", argNullEx);
//                }
//                catch (Exception e)
//                {
//                    _logger.ErrorException("Exception handled sending episodes to trakt.tv", e);
//                }
//            }
//
//            if (playedMovies.Count > 0)
//            {
//                try
//                {
//                    var dataContract = await _traktApi.SendMoviePlaystateUpdates(playedMovies, traktUser, true, cancellationToken);
//                    if (dataContract != null)
//                        LogTraktResponseDataContract(dataContract);
//                }
//                catch (Exception e)
//                {
//                    _logger.ErrorException("Error updating movie play states", e);
//                }
//            }
//
//            if (playedEpisodes.Count > 0)
//            {
//                try
//                {
//                    var dataContracts = await _traktApi.SendEpisodePlaystateUpdates(playedEpisodes, traktUser, true, cancellationToken);
//                    if (dataContracts != null)
//                    {
//                        foreach (var dataContract in dataContracts)
//                            LogTraktResponseDataContract(dataContract);
//                    }
//                }
//                catch (Exception e)
//                {
//                    _logger.ErrorException("Error updating episode play states", e);
//                }
//            }
//
//            if (unPlayedEpisodes.Count > 0)
//            {
//                try
//                {
//                    var dataContracts = await _traktApi.SendEpisodePlaystateUpdates(unPlayedEpisodes, traktUser, false, cancellationToken);
//                    if (dataContracts != null)
//                    {
//                        foreach (var dataContract in dataContracts)
//                        {
//                            LogTraktResponseDataContract(dataContract);
//                        }
//                    }
//                }
//                catch (Exception e)
//                {
//                    _logger.ErrorException("Error updating episode play states", e);
//                }
//            }
//        }
//
//        public string Name
//        {
//            get { return "Sync library to trakt.tv"; }
//        }
//
//        public string Category
//        {
//            get
//            {
//                return "Trakt";
//            }
//        }
//
//        public string Description
//        {
//            get
//            {
//                return
//                    "Adds any media that is in each users trakt monitored locations to their trakt.tv profile";
//            }
//        }
//
//        private void LogTraktResponseDataContract(TraktResponseDataContract dataContract)
//        {
//            _logger.Debug("TraktResponse status: " + dataContract.Status);
//            if (dataContract.Status.Equals("failure", StringComparison.OrdinalIgnoreCase))
//                _logger.Error("TraktResponse error: " + dataContract.Error);
//            _logger.Debug("TraktResponse message: " + dataContract.Message);
//        }
//    }
//}
