using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.NextPvr.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string WebServiceUrl { get; set; }
        public int Port { get; set; }
        public string Pin { get; set; }
    }
}
