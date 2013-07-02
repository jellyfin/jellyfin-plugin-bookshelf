using System;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Theater.Interfaces.Playback;
using MediaBrowser.Theater.Interfaces.UserInput;
using MediaBrowser.Theater.Presentation.Playback;
using System.Threading.Tasks;
using System.Linq;

namespace MediaBrowser.Plugins.MpcHc
{
    public class MpcHcExternalPlayer : GenericExternalPlayer
    {
        public MpcHcExternalPlayer(IPlaybackManager playbackManager, ILogger logger, IUserInputManager userInput)
            : base(playbackManager, logger, userInput)
        {
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

        public override PlayState PlayState
        {
            get
            {
                return base.PlayState;
            }
        }

        public override long? CurrentPositionTicks
        {
            get
            {
                return base.CurrentPositionTicks;
            }
        }

        public override string Name
        {
            get
            {
                return "MpcHc";
            }
        }

        public override Task Pause()
        {
            return base.Pause();
        }

        public override Task UnPause()
        {
            return base.UnPause();
        }

        public override Task Stop()
        {
            return base.Stop();
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
            return base.Seek(positionTicks);
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

    }
}
