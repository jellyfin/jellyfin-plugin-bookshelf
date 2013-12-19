using System.Linq;
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
            _libraryManager.ItemUpdated += _libraryManager_ItemUpdated;
        }

        void _libraryManager_ItemUpdated(object sender, ItemChangeEventArgs e)
        {
            if (e.UpdateReason == ItemUpdateType.ImageUpdate && e.Item is Person)
            {
                var person = e.Item.Name;

                var items = _libraryManager.RootFolder
                    .GetRecursiveChildren(i => !i.IsFolder && i.People.Any(p => string.Equals(p.Name, person, StringComparison.OrdinalIgnoreCase)));

                foreach (var item in items)
                {
                    SaveMetadataForItem(item, ItemUpdateType.MetadataEdit);
                }
            }
        }

        void _userDataManager_UserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            if (e.SaveReason == UserDataSaveReason.PlaybackFinished || e.SaveReason == UserDataSaveReason.TogglePlayed)
            {
                var item = e.Item as BaseItem;

                if (item != null)
                {
                    if (!item.IsFolder && !(item is IItemByName))
                    {
                        SaveMetadataForItem(item, ItemUpdateType.MetadataEdit);
                    }
                }
            }
        }

        public void Dispose()
        {
            _userDataManager.UserDataSaved -= _userDataManager_UserDataSaved;
        }

        private async void SaveMetadataForItem(BaseItem item, ItemUpdateType updateReason)
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
                await _libraryManager.SaveMetadata(item, updateReason).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving metadata for {0}", ex, item.Path ?? item.Name);
            }
        }
    }
}
