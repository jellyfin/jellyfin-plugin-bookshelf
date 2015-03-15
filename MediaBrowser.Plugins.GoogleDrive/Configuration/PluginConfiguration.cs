using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            ApplyConfigurationToEveryone = true;
            Users = new List<GoogleDriveUser>();
        }

        public string GoogleDriveClientId { get; set; }
        public string GoogleDriveClientSecret { get; set; }

        public bool ApplyConfigurationToEveryone { get; set; }
        public GoogleDriveUser SingleUserForEveryone { get; set; }
        public List<GoogleDriveUser> Users { get; set; }
    }
}
