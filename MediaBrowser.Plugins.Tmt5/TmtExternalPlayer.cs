using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Theater.Interfaces.Playback;
using MediaBrowser.Theater.Interfaces.UserInput;
using MediaBrowser.Theater.Presentation.Playback;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Tmt5
{
    public class TmtExternalPlayer : GenericExternalPlayer, IDisposable
    {
        public TmtExternalPlayer(IPlaybackManager playbackManager, ILogger logger, IUserInputManager userInput)
            : base(playbackManager, logger, userInput)
        {
        }

        /// <summary>
        /// Gets or sets the status file watcher.
        /// </summary>
        /// <value>The status file watcher.</value>
        private FileSystemWatcher StatusFileWatcher { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has started playing.
        /// </summary>
        /// <value><c>true</c> if this instance has started playing; otherwise, <c>false</c>.</value>
        private bool HasStartedPlaying { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has stopped playing.
        /// </summary>
        /// <value><c>true</c> if this instance has stopped playing; otherwise, <c>false</c>.</value>
        private bool HasStoppedPlaying { get; set; }

        public override bool CanPlayMediaType(string mediaType)
        {
            return string.Equals(mediaType, MediaType.Video);
        }

        public override bool CanPause
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

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool RequiresConfiguredArguments
        {
            get
            {
                return false;
            }
        }

        public override bool CanCloseAutomaticallyOnStopButton
        {
            get
            {
                return true;
            }
        }

        public override string Name
        {
            get
            {
                return "Total Media Theatre";
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

        protected override void OnPlayerLaunched()
        {
            base.OnPlayerLaunched();

            // If the playstate directory exists, start watching it
            if (Directory.Exists(TmtService.PlayStateDirectory))
            {
                ReloadFileSystemWatcher();
            }
        }

        public override Task Pause()
        {
            return SendCommandToMMC("-pause");
        }

        public override Task Stop()
        {
            return SendCommandToMMC("-stop");
        }

        public override Task UnPause()
        {
            return SendCommandToMMC("-play");
        }

        public override Task Seek(long positionTicks)
        {
            if (CurrentMedia == null)
            {
                throw new InvalidOperationException("No media to seek to");
            }

            if (CurrentMedia.Chapters == null)
            {
                throw new InvalidOperationException("TMT5 cannot seek without chapter information");
            }

            var chapterIndex = 0;

            for (var i = 0; i < CurrentMedia.Chapters.Count; i++)
            {
                if (CurrentMedia.Chapters[i].StartPositionTicks < positionTicks)
                {
                    chapterIndex = i;
                }
            }

            return JumpToChapter(chapterIndex);
        }

        /// <summary>
        /// Jumps to chapter.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <returns>Task.</returns>
        protected Task JumpToChapter(int chapter)
        {
            return SendCommandToMMC(" -chapter " + chapter);
        }

        /// <summary>
        /// Closes the player.
        /// </summary>
        /// <returns>Task.</returns>
        protected Task ClosePlayer()
        {
            return SendCommandToMMC("-close");
        }

        /// <summary>
        /// Sends an arbitrary command to the TMT MMC console
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>Task.</returns>
        protected Task SendCommandToMMC(string command)
        {
            return Task.Run(() =>
            {
                var directory = Path.GetDirectoryName(CurrentPlayOptions.Configuration.Command);

                var processInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(directory, "MMCEDT5.exe"),
                    Arguments = command,
                    CreateNoWindow = true
                };

                Logger.Debug("{0} {1}", processInfo.FileName, processInfo.Arguments);

                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit(2000);
                }
            });
        }

        /// <summary>
        /// Reloads the file system watcher.
        /// </summary>
        private void ReloadFileSystemWatcher()
        {
            DisposeFileSystemWatcher();

            Logger.Info("Watching TMT folder: " + TmtService.PlayStateDirectory);

            StatusFileWatcher = new FileSystemWatcher(TmtService.PlayStateDirectory, "*.set")
            {
                IncludeSubdirectories = true
            };

            // Need to include subdirectories since there are subfolders undearneath this with the TMT version #.
            StatusFileWatcher.Changed += StatusFileWatcher_Changed;
            StatusFileWatcher.EnableRaisingEvents = true;
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Handles the Changed event of the StatusFileWatcher control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FileSystemEventArgs" /> instance containing the event data.</param>
        async void StatusFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Logger.Debug("TMT File Watcher reports change type {1} at {0}", e.FullPath, e.ChangeType);

            NameValueCollection values;

            try
            {
                values = TmtService.ParseIniFile(e.FullPath);
            }
            catch (IOException)
            {
                // This can happen if the file is being written to at the exact moment we're trying to access it
                // Unfortunately we kind of have to just eat it
                return;
            }

            var tmtPlayState = values["State"];

            if (tmtPlayState.Equals("play", StringComparison.OrdinalIgnoreCase))
            {
                _isPaused = false;

                // Playback just started
                HasStartedPlaying = true;

                if (CurrentPlayOptions.StartPositionTicks > 0)
                {
                    await Seek(CurrentPlayOptions.StartPositionTicks).ConfigureAwait(false);
                }
            }
            else if (tmtPlayState.Equals("pause", StringComparison.OrdinalIgnoreCase))
            {
                _isPaused = true;
            }

            // If playback has previously started...
            // First notify the Progress event handler
            // Then check if playback has stopped
            if (HasStartedPlaying)
            {
                TimeSpan currentPosition;

                //TimeSpan.TryParse(values["TotalTime"], out currentDuration);

                if (TimeSpan.TryParse(values["CurTime"], UsCulture, out currentPosition))
                {
                    CurrentPositionTicks = currentPosition.Ticks;
                }

                // Playback has stopped
                if (tmtPlayState.Equals("stop", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("Playstate changed to stopped");

                    if (!HasStoppedPlaying)
                    {
                        HasStoppedPlaying = true;

                        DisposeFileSystemWatcher();

                        await ClosePlayer().ConfigureAwait(false);
                    }
                }
            }
        }

        protected override void OnPlayerExited()
        {
            base.OnPlayerExited();

            DisposeFileSystemWatcher();
            HasStartedPlaying = false;
            HasStoppedPlaying = false;
            _isPaused = false;
        }

        /// <summary>
        /// Disposes the file system watcher.
        /// </summary>
        private void DisposeFileSystemWatcher()
        {
            if (StatusFileWatcher != null)
            {
                StatusFileWatcher.EnableRaisingEvents = false;
                StatusFileWatcher.Changed -= StatusFileWatcher_Changed;
                StatusFileWatcher.Dispose();
                StatusFileWatcher = null;
            }
        }

        public void Dispose()
        {
            DisposeFileSystemWatcher();
        }
    }
}
