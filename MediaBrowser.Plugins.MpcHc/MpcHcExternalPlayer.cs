using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Theater.Interfaces.Configuration;
using MediaBrowser.Theater.Interfaces.Playback;
using MediaBrowser.Theater.Interfaces.UserInput;
using MediaBrowser.Theater.Presentation.Playback;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.MpcHc
{
    public class MpcHcExternalPlayer : GenericExternalPlayer
    {
        private CancellationTokenSource HttpInterfaceCancellationTokenSource { get; set; }

        private bool HasStartedPlaying { get; set; }

        private Timer StatusUpdateTimer { get; set; }

        private readonly IHttpClient _httpClient;

        private readonly object _stateSyncLock = new object();

        private readonly SemaphoreSlim _mpcHttpInterfaceResourcePool = new SemaphoreSlim(1, 1);

        public MpcHcExternalPlayer(IPlaybackManager playbackManager, ILogger logger, IUserInputManager userInput, IHttpClient httpClient)
            : base(playbackManager, logger, userInput)
        {
            _httpClient = httpClient;
        }

        public override bool CanCloseAutomaticallyOnStopButton
        {
            get
            {
                return true;
            }
        }

        public override bool CanPause
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanTrackProgress
        {
            get
            {
                return true;
            }
        }

        private bool _isPaused;
        public override PlayState PlayState
        {
            get
            {
                if (_isPaused)
                {
                    return PlayState.Paused;
                }
                return base.PlayState;
            }
        }

        public override string Name
        {
            get
            {
                return "Mpc-Hc";
            }
        }

        public override Task Pause()
        {
            return SendCommandToPlayer("888", new Dictionary<string, string>());
        }

        public override Task UnPause()
        {
            return SendCommandToPlayer("887", new Dictionary<string, string>());
        }

        public override Task Stop()
        {
            return SendCommandToPlayer("890", new Dictionary<string, string>());
        }

        public override bool SupportsMultiFilePlayback
        {
            get
            {
                return true;
            }
        }

        public override Task Seek(long positionTicks)
        {
            var additionalParams = new Dictionary<string, string>();

            var time = TimeSpan.FromTicks(positionTicks);

            var timeString = time.Hours + ":" + time.Minutes + ":" + time.Seconds;

            additionalParams.Add("position", timeString);

            return SendCommandToPlayer("-1", additionalParams);
        }

        public override bool RequiresConfiguredArguments
        {
            get
            {
                return false;
            }
        }

        public override bool CanPlayMediaType(string mediaType)
        {
            return new[] { MediaType.Video, MediaType.Audio }.Contains(mediaType, StringComparer.OrdinalIgnoreCase);
        }

        protected Task ClosePlayer()
        {
            return SendCommandToPlayer("816", new Dictionary<string, string>());
        }

        protected override string GetCommandArguments(IEnumerable<BaseItemDto> items, PlayOptions options)
        {
            var formatString = "{0} /play /fullscreen /close";

            var firstItem = items.First();

            var startTicks = Math.Max(options.StartPositionTicks, 0);

            if (startTicks > 0 && firstItem.IsVideo && firstItem.VideoType.HasValue && firstItem.VideoType.Value == VideoType.Dvd)
            {
                formatString += " /dvdpos 1#" + TimeSpan.FromTicks(startTicks).ToString("hh\\:mm\\:ss");
            }
            else
            {
                formatString += " /start " + TimeSpan.FromTicks(startTicks).TotalMilliseconds;
            }

            return GetCommandArguments(items, formatString);
        }

        /// <summary>
        /// Gets the path for command line.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected override string GetPathForCommandLine(BaseItemDto item)
        {
            var path = base.GetPathForCommandLine(item);

            if (item.IsVideo && item.VideoType.HasValue)
            {
                if (item.VideoType.Value == VideoType.Dvd)
                {
                    // Point directly to the video_ts path
                    // Otherwise mpc will play any other media files that might exist in the dvd top folder (e.g. video backdrops).
                    var videoTsPath = Path.Combine(path, "video_ts");

                    if (Directory.Exists(videoTsPath))
                    {
                        path = videoTsPath;
                    }
                }
                if (item.VideoType.Value == VideoType.BluRay)
                {
                    // Point directly to the bdmv path
                    var bdmvPath = Path.Combine(path, "bdmv");

                    if (Directory.Exists(bdmvPath))
                    {
                        path = bdmvPath;
                    }
                }
            }

            return FormatPath(path);
        }

        /// <summary>
        /// Formats the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        private string FormatPath(string path)
        {
            if (path.EndsWith(":\\", StringComparison.OrdinalIgnoreCase))
            {
                path = path.TrimEnd('\\');
            }

            return path;
        }

        protected override void OnPlayerLaunched()
        {
            base.OnPlayerLaunched();

            ReloadStatusUpdateTimer();
        }

        /// <summary>
        /// Reloads the status update timer.
        /// </summary>
        private void ReloadStatusUpdateTimer()
        {
            DisposeStatusTimer();

            HttpInterfaceCancellationTokenSource = new CancellationTokenSource();

            StatusUpdateTimer = new Timer(OnStatusUpdateTimerStopped, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Called when [status update timer stopped].
        /// </summary>
        /// <param name="state">The state.</param>
        private async void OnStatusUpdateTimerStopped(object state)
        {
            try
            {
                var token = HttpInterfaceCancellationTokenSource.Token;

                using (var stream = await _httpClient.Get(MpcHcService.StatusUrl, _mpcHttpInterfaceResourcePool, token).ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();

                    using (var reader = new StreamReader(stream))
                    {
                        token.ThrowIfCancellationRequested();

                        var result = await reader.ReadToEndAsync().ConfigureAwait(false);

                        token.ThrowIfCancellationRequested();

                        ProcessStatusResult(result);
                    }
                }
            }
            catch (HttpException ex)
            {
                Logger.ErrorException("Error connecting to MpcHc status interface", ex);
            }
            catch (OperationCanceledException)
            {
                // Manually cancelled by us
                Logger.Info("Status request cancelled");
            }
        }

        /// <summary>
        /// Processes the status result.
        /// </summary>
        /// <param name="result">The result.</param>
        private async void ProcessStatusResult(string result)
        {
            // Sample result
            // OnStatus('test.avi', 'Playing', 5292, '00:00:05', 1203090, '00:20:03', 0, 100, 'C:\test.avi')
            // 5292 = position in ms
            // 00:00:05 = position
            // 1203090 = duration in ms
            // 00:20:03 = duration

            var quoteChar = result.IndexOf(", \"", StringComparison.OrdinalIgnoreCase) == -1 ? '\'' : '\"';

            // Strip off the leading "OnStatus(" and the trailing ")"
            result = result.Substring(result.IndexOf(quoteChar));
            result = result.Substring(0, result.LastIndexOf(quoteChar));

            // Strip off the filename at the beginning
            result = result.Substring(result.IndexOf(string.Format("{0}, {0}", quoteChar), StringComparison.OrdinalIgnoreCase) + 3);

            // Find the last index of ", '" so that we can extract and then strip off the file path at the end.
            var lastIndexOfSeparator = result.LastIndexOf(", " + quoteChar, StringComparison.OrdinalIgnoreCase);

            // Get the current playing file path
            var currentPlayingFile = result.Substring(lastIndexOfSeparator + 2).Trim(quoteChar);

            // Strip off the current playing file path
            result = result.Substring(0, lastIndexOfSeparator);

            var values = result.Split(',').Select(v => v.Trim().Trim(quoteChar)).ToList();

            var currentPositionTicks = TimeSpan.FromMilliseconds(double.Parse(values[1])).Ticks;
            //var currentDurationTicks = TimeSpan.FromMilliseconds(double.Parse(values[3])).Ticks;

            var playstate = values[0];

            var playlistIndex = GetPlaylistIndex(currentPlayingFile);

            if (playstate.Equals("stopped", StringComparison.OrdinalIgnoreCase))
            {
                if (HasStartedPlaying)
                {
                    await ClosePlayer().ConfigureAwait(false);
                }
            }
            else
            {
                lock (_stateSyncLock)
                {
                    if (CurrentPlaylistIndex != playlistIndex)
                    {
                        //OnMediaChanged(CurrentPlaylistIndex, CurrentPositionTicks, playlistIndex);
                        //EventHelper.QueueEventIfNotNull(MediaChanged, this, new MediaChangeEventArgs
                        //    {
                        //        Player = this
                        //    }, Logger);
                    }

                    CurrentPositionTicks = currentPositionTicks;
                    CurrentPlaylistIndex = playlistIndex;
                }

                if (playstate.Equals("playing", StringComparison.OrdinalIgnoreCase))
                {
                    HasStartedPlaying = true;
                    _isPaused = false;
                }
                else if (playstate.Equals("paused", StringComparison.OrdinalIgnoreCase))
                {
                    HasStartedPlaying = true;
                    _isPaused = true;
                }
            }
        }

        /// <summary>
        /// Gets the index of the playlist.
        /// </summary>
        /// <param name="nowPlayingPath">The now playing path.</param>
        /// <returns>System.Int32.</returns>
        private int GetPlaylistIndex(string nowPlayingPath)
        {
            for (var i = 0; i < Playlist.Count; i++)
            {
                var item = Playlist[i];

                var pathArg = GetPathForCommandLine(item);

                if (pathArg.Equals(nowPlayingPath, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }

                if (item.VideoType.HasValue)
                {
                    if (item.VideoType.Value == VideoType.BluRay || item.VideoType.Value == VideoType.Dvd || item.VideoType.Value == VideoType.HdDvd)
                    {
                        if (nowPlayingPath.StartsWith(pathArg, StringComparison.OrdinalIgnoreCase))
                        {
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Sends a command to MPC using the HTTP interface
        /// http://www.autohotkey.net/~specter333/MPC/HTTP%20Commands.txt
        /// </summary>
        /// <param name="commandNumber">The command number.</param>
        /// <param name="additionalParams">The additional params.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">commandNumber</exception>
        private async Task SendCommandToPlayer(string commandNumber, Dictionary<string, string> additionalParams)
        {
            if (string.IsNullOrEmpty(commandNumber))
            {
                throw new ArgumentNullException("commandNumber");
            }

            if (additionalParams == null)
            {
                throw new ArgumentNullException("additionalParams");
            }

            var url = MpcHcService.CommandUrl + "?wm_command=" + commandNumber;

            url = additionalParams.Keys.Aggregate(url, (current, name) => current + ("&" + name + "=" + additionalParams[name]));

            Logger.Info("Sending command to MPC: " + url);

            try
            {
                using (var stream = await _httpClient.Get(url, _mpcHttpInterfaceResourcePool, HttpInterfaceCancellationTokenSource.Token).ConfigureAwait(false))
                {
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.ErrorException("Error connecting to MpcHc command interface", ex);
            }
            catch (OperationCanceledException)
            {
                // Manually cancelled by us
                Logger.Info("Command request cancelled");
            }
        }

        protected override void OnPlayerExited()
        {
            base.OnPlayerExited();

            HttpInterfaceCancellationTokenSource.Cancel();

            DisposeStatusTimer();
            _isPaused = false;
            HasStartedPlaying = false;
            HttpInterfaceCancellationTokenSource = null;
        }

        /// <summary>
        /// Disposes the status timer.
        /// </summary>
        private void DisposeStatusTimer()
        {
            if (StatusUpdateTimer != null)
            {
                StatusUpdateTimer.Dispose();
            }
        }

        public void Dispose()
        {
            DisposeStatusTimer();

            _mpcHttpInterfaceResourcePool.Dispose();
        }
    }
}
