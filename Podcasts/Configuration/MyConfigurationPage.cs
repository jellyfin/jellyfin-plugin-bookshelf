using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using System;
using System.IO;

namespace PodCasts.Configuration
{
    /// <summary>
    /// Class MyConfigurationPage
    /// </summary>
    class MyConfigurationPage : IPluginConfigurationPage
    {
        /// <summary>
        /// Gets My Option.
        /// </summary>
        /// <value>The Option.</value>
        public string Name
        {
            get { return "PodCasts"; }
        }

        /// <summary>
        /// Gets the HTML stream.
        /// </summary>
        /// <returns>Stream.</returns>
        public Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream("PodCasts.Configuration.configPage.html");
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
            get { return PodCasts.Plugin.Instance; }
        }
    }
}
