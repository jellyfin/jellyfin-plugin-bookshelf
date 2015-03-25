using System;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;

namespace MediaBrowser.Plugins.EmbyTV.Configuration
{
    class EmbyTVConfigurationPage : IPluginConfigurationPage
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
            return GetType().Assembly.GetManifestResourceStream("EmbyTV.Configuration.configPage.html");
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "EmbyTV"; }
        }

        public IPlugin Plugin
        {
            get { return MediaBrowser.Plugins.EmbyTV.Plugin.Instance; }
        }
    }
}
