using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.LocalTrailers.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether [enable local trailer downloads].
        /// </summary>
        /// <value><c>true</c> if [enable local trailer downloads]; otherwise, <c>false</c>.</value>
        public bool EnableLocalTrailerDownloads { get; set; }
    }
}
