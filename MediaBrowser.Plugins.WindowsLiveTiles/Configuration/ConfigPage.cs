using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using System.IO;

namespace MediaBrowser.Plugins.WindowsLiveTiles.Configuration
{
    class ConfigPage : IPluginConfigurationPage
    {
        /// <summary>
        /// Gets My Option.
        /// </summary>
        /// <value>The Option.</value>
        public string Name
        {
            get { return Plugin.Name; }
        }

        /// <summary>
        /// Gets the HTML stream.
        /// </summary>
        /// <returns>Stream.</returns>
        public Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream(GetType().Namespace + ".config.html");
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
            get { return WindowsLiveTiles.Plugin.Instance; }
        }
    }
}
