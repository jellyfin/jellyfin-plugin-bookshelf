using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Dropbox.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            SyncAccounts = new List<DropboxSyncAccount>();
        }

        public string DropboxAppKey { get; set; }
        public string DropboxAppSecret { get; set; }
        public List<DropboxSyncAccount> SyncAccounts { get; set; }
    }
}
