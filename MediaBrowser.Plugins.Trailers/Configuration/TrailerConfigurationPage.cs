using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using System;
using System.IO;

namespace MediaBrowser.Plugins.Trailers.Configuration
{
    /// <summary>
    /// Class TrailerConfigurationPage
    /// </summary>
    class TrailerConfigurationPage : IPluginConfigurationPage
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Trailers"; }
        }

        /// <summary>
        /// Gets the HTML stream.
        /// </summary>
        /// <returns>Stream.</returns>
        public Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream("MediaBrowser.Plugins.Trailers.Configuration.configPage.html");
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
            get { return Trailers.Plugin.Instance; }
        }
    }
}
