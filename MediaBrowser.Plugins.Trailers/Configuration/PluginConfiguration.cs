using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.Trailers.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Trailers older than this will not be downloaded and deleted if already downloaded.
        /// </summary>
        /// <value>The max trailer age.</value>
        public int? MaxTrailerAge { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable local trailer downloads].
        /// </summary>
        /// <value><c>true</c> if [enable local trailer downloads]; otherwise, <c>false</c>.</value>
        public bool EnableLocalTrailerDownloads { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public PluginConfiguration()
            : base()
        {
            MaxTrailerAge = 60;
        }
    }
}
