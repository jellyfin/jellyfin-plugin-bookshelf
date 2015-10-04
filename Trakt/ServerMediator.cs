using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Linq;
using CommonIO;
using Trakt.Api;
using Trakt.Helpers;

namespace Trakt
{
    /// <summary>
    /// All communication between the server and the plugins server instance should occur in this class.
    /// </summary>
    public class ServerMediator : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private TraktApi _traktApi;
        private TraktUriService _service;
        private LibraryManagerEventsHelper _libraryManagerEventsHelper;
        private readonly UserDataManagerEventsHelper _userDataManagerEventsHelper;

        public static ServerMediator Instance { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonSerializer"></param>
        /// <param name="sessionManager"> </param>
        /// <param name="userDataManager"></param>
        /// <param name="libraryManager"> </param>
        /// <param name="logger"></param>
        /// <param name="httpClient"></param>
        /// <param name="appHost"></param>
        /// <param name="fileSystem"></param>
        public ServerMediator(IJsonSerializer jsonSerializer, ISessionManager sessionManager, IUserDataManager userDataManager, ILibraryManager libraryManager, ILogManager logger, IHttpClient httpClient, IServerApplicationHost appHost, IFileSystem fileSystem)
        {
            Instance = this;
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _logger = logger.GetLogger("Trakt");

            _traktApi = new TraktApi(jsonSerializer, _logger, httpClient, appHost, userDataManager, fileSystem);
            _service = new TraktUriService(_traktApi, _logger, _libraryManager);
            _libraryManagerEventsHelper = new LibraryManagerEventsHelper(_logger, _traktApi);
            _userDataManagerEventsHelper = new UserDataManagerEventsHelper(_logger, _traktApi);

            userDataManager.UserDataSaved += _userDataManager_UserDataSaved;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _userDataManager_UserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            // ignore change events for any reason other than manually toggling played.
            if (e.SaveReason != UserDataSaveReason.TogglePlayed) return;

            var baseItem = e.Item as BaseItem;

            if (baseItem != null)
            {
                // determine if user has trakt credentials
                var traktUser = UserHelper.GetTraktUser(e.UserId.ToString());

                // Can't progress
                if (traktUser == null || !_traktApi.CanSync(baseItem, traktUser))
                    return;

                // We have a user and the item is in a trakt monitored location. 
                _userDataManagerEventsHelper.ProcessUserDataSaveEventArgs(e, traktUser);
            }
        }



        /// <summary>
        /// 
        /// </summary>
        public void Run()
        {
            _sessionManager.PlaybackStart += KernelPlaybackStart;
            _sessionManager.PlaybackStopped += KernelPlaybackStopped;
            _libraryManager.ItemAdded += LibraryManagerItemAdded;
            _libraryManager.ItemRemoved += LibraryManagerItemRemoved;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LibraryManagerItemRemoved(object sender, ItemChangeEventArgs e)
        {
            if (!(e.Item is Movie) && !(e.Item is Episode) && !(e.Item is Series)) return;
            if (e.Item.LocationType == LocationType.Virtual) return;
            _libraryManagerEventsHelper.QueueItem(e.Item, EventType.Remove);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LibraryManagerItemAdded(object sender, ItemChangeEventArgs e)
        {
            // Don't do anything if it's not a supported media type
            if (!(e.Item is Movie) && !(e.Item is Episode) && !(e.Item is Series)) return;
            if (e.Item.LocationType == LocationType.Virtual) return;
            _libraryManagerEventsHelper.QueueItem(e.Item, EventType.Add);
        }



        /// <summary>
        /// Let Trakt.tv know the user has started to watch something
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void KernelPlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            try
            {
                _logger.Info("Playback Started");

                if (e.Users == null || !e.Users.Any() || e.Item == null)
                {
                    _logger.Error("Event details incomplete. Cannot process current media");
                    return;
                }

                // Since MB3 is user profile friendly, I'm going to need to do a user lookup every time something starts
                var traktUser = UserHelper.GetTraktUser(e.Users.FirstOrDefault());

                if (traktUser == null)
                {
                    _logger.Info("Could not match user with any stored credentials");
                    return;
                }

                if (!_traktApi.CanSync(e.Item, traktUser))
                {
                    return;
                }

                _logger.Debug(traktUser.LinkedMbUserId + " appears to be monitoring " + e.Item.Path);

                var video = e.Item as Video;
                var progressPercent = video.RunTimeTicks.HasValue && video.RunTimeTicks != 0 ? 
                    (float)(e.PlaybackPositionTicks??0) / video.RunTimeTicks.Value * 100.0f : 0.0f;

                try
                {
                    if (video is Movie)
                    {
                        _logger.Debug("Send movie status update");
                        await
                            _traktApi.SendMovieStatusUpdateAsync(video as Movie, MediaStatus.Watching, traktUser, progressPercent).
                                      ConfigureAwait(false);
                    }
                    else if (video is Episode)
                    {
                        _logger.Debug("Send episode status update");
                        await
                            _traktApi.SendEpisodeStatusUpdateAsync(video as Episode, MediaStatus.Watching, traktUser, progressPercent).
                                      ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Exception handled sending status update", ex);
                }

                var playEvent = new ProgressEvent
                {
                    UserId = e.Users.First().Id,
                    ItemId = e.Item.Id,
                    LastApiAccess = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending watching status update", ex, null);
            }
        }

        /// <summary>
        /// Media playback has stopped. Depending on playback progress, let Trakt.tv know the user has
        /// completed watching the item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void KernelPlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            if (e.Users == null || !e.Users.Any() || e.Item == null)
            {
                _logger.Error("Event details incomplete. Cannot process current media");
                return;
            }

            try
            {
                _logger.Info("Playback Stopped");
                var traktUser = UserHelper.GetTraktUser(e.Users.FirstOrDefault());

                if (traktUser == null)
                {
                    _logger.Error("Could not match trakt user");
                    return;
                }

                if (!_traktApi.CanSync(e.Item, traktUser))
                {
                    return;
                }

                var video = e.Item as Video;

                if (e.PlayedToCompletion)
                {
                    _logger.Info("Item is played. Scrobble");

                    try
                    {
                        if (video is Movie)
                        {
                            await
                                _traktApi.SendMovieStatusUpdateAsync(video as Movie, MediaStatus.Stop, traktUser, 100).
                                    ConfigureAwait(false);
                        }
                        else if (video is Episode)
                        {
                            await
                                _traktApi.SendEpisodeStatusUpdateAsync(video as Episode, MediaStatus.Stop, traktUser, 100)
                                    .ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Exception handled sending status update", ex);
                    }

                }
                else
                {
                    var progressPercent = video.RunTimeTicks.HasValue && video.RunTimeTicks != 0 ?
                    (float)(e.PlaybackPositionTicks ?? 0) / video.RunTimeTicks.Value * 100.0f : 0.0f;
                    _logger.Info("Item Not fully played. Tell trakt.tv we are no longer watching but don't scrobble");

                    if (video is Movie)
                    {
                        await _traktApi.SendMovieStatusUpdateAsync(video as Movie, MediaStatus.Stop, traktUser, progressPercent);
                    }
                    else
                    {
                        await _traktApi.SendEpisodeStatusUpdateAsync(video as Episode, MediaStatus.Stop, traktUser, progressPercent);
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending scrobble", ex, null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _sessionManager.PlaybackStart -= KernelPlaybackStart;
            _sessionManager.PlaybackStopped -= KernelPlaybackStopped;
            _libraryManager.ItemAdded -= LibraryManagerItemAdded;
            _libraryManager.ItemRemoved -= LibraryManagerItemRemoved;
            _service = null;
            _traktApi = null;
            _libraryManagerEventsHelper = null;
        }
    }



    /// <summary>
    /// 
    /// </summary>
    public class ProgressEvent
    {
        public Guid UserId;
        public Guid ItemId;
        public DateTime LastApiAccess;
    }
}