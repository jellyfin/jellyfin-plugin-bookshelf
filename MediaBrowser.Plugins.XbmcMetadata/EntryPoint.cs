using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Threading;

namespace MediaBrowser.Plugins.XbmcMetadata
{
    public class EntryPoint : IServerEntryPoint
    {
        private readonly IUserDataManager _userDataManager;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;

        public EntryPoint(IUserDataManager userDataManager, ILibraryManager libraryManager, ILogger logger)
        {
            _userDataManager = userDataManager;
            _libraryManager = libraryManager;
            _logger = logger;
        }

        public void Run()
        {
            _userDataManager.UserDataSaved += _userDataManager_UserDataSaved;
        }

        void _userDataManager_UserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            if (e.SaveReason == UserDataSaveReason.PlaybackFinished || e.SaveReason == UserDataSaveReason.TogglePlayed)
            {
                SaveMetadataForItem(e.Item, CancellationToken.None);
            }
        }

        public void Dispose()
        {
            _userDataManager.UserDataSaved -= _userDataManager_UserDataSaved;
        }

        private async void SaveMetadataForItem(BaseItem item, CancellationToken cancellationToken)
        {
            var userId = Plugin.Instance.Configuration.UserId;

            if (!userId.HasValue)
            {
                return;
            }

            var locationType = item.LocationType;
            if (locationType == LocationType.Remote ||
                locationType == LocationType.Virtual)
            {
                return;
            }

            try
            {
                //await _libraryManager.SaveMetadata(item, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving metadata for {0}", ex, item.Path ?? item.Name);
            }
        }
    }
}
