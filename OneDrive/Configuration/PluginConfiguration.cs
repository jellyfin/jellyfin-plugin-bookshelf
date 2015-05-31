using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace OneDrive.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            SyncAccounts = new List<OneDriveSyncAccount>();
        }

        public string OneDriveClientId { get; set; }
        public string OneDriveClientSecret { get; set; }
        public List<OneDriveSyncAccount> SyncAccounts { get; set; }
    }
}
