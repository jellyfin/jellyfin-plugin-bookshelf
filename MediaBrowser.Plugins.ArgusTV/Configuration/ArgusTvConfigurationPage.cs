using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using System.IO;

namespace MediaBrowser.Plugins.ArgusTV.Configuration
{
    /// <summary>
    /// Class MyConfigurationPage
    /// </summary>
    class ArgusTvConfigurationPage : IPluginConfigurationPage
    {
        /// <summary>
        /// Gets My Option.
        /// </summary>
        /// <value>The Option.</value>
        public string Name
        {
            get { return "ArgusTV"; }
        }

        /// <summary>
        /// Gets the HTML stream.
        /// </summary>
        /// <returns>Stream.</returns>
        public Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream("MediaBrowser.Plugins.ArgusTV.Configuration.configPage.html");
        }

        /// <summary>
        /// Gets the type of the configuration page.
        /// </summary>
        /// <value>The type of the configuration page.</value>
        public ConfigurationPageType ConfigurationPageType
        {
            get { return ConfigurationPageType.PluginConfiguration; }
        }

        public IPlugin Plugin
        {
            get { return ArgusTV.Plugin.Instance; }
        }
    }
}
