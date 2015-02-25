using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            Users = new List<GoogleDriveUser>();
        }

        public List<GoogleDriveUser> Users { get; set; }
    }
}
