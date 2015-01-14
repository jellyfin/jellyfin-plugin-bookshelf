using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.StudioCleaner.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public StudioOptions MovieOptions { get; set; }
        public StudioOptions SeriesOptions { get; set; }
        public string LastChangedOption { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public PluginConfiguration()
        {
            MovieOptions = new StudioOptions();
            SeriesOptions = new StudioOptions();
        }
    }
}
