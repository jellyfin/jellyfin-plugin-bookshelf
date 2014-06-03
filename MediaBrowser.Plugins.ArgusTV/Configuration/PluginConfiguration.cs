using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.ArgusTV.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string ServerIp { get; set; }
        public int ServerPort { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public PluginConfiguration()
        {
            ServerIp = "localhost";
            ServerPort = 49943;
        }
    }
}
