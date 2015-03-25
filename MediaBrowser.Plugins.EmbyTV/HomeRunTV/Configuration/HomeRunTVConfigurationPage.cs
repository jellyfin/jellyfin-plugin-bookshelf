using System;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;

namespace MediaBrowser.Plugins.EmbyTV.Configuration
{
    class HomeRunTVConfigurationPage : IPluginConfigurationPage
    {
        /// <summary>
        /// Gets the type of the configuration page.
        /// </summary>
        /// <value>The type of the configuration page.</value>
        public ConfigurationPageType ConfigurationPageType
        {
            get { return ConfigurationPageType.PluginConfiguration; }
        }
        
        /// <summary>
        /// Gets the HTML stream.
        /// </summary>
        /// <returns>Stream.</returns>
        public System.IO.Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream("HomeRunTV.Configuration.configPage.html");
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "HomeRunTV"; }
        }

        public IPlugin Plugin
        {
            get { return MediaBrowser.Plugins.EmbyTV.Plugin.Instance; }
        }
    }
}
