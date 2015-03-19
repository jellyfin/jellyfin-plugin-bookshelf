using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            SyncAccounts = new List<GoogleDriveSyncAccount>();
        }

        public string GoogleDriveClientId { get; set; }
        public string GoogleDriveClientSecret { get; set; }
        public List<GoogleDriveSyncAccount> SyncAccounts { get; set; }
    }
}
