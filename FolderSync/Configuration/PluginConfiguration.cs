using MediaBrowser.Model.Plugins;

namespace FolderSync.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public SyncAccount[] SyncAccounts { get; set; }

        public PluginConfiguration()
        {
            SyncAccounts = new SyncAccount[] { };
        }
    }
}
