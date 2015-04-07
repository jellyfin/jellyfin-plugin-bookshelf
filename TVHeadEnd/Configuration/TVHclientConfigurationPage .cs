using System.IO;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;

namespace TVHeadEnd.Configuration
{
    /// <summary>
    /// Class TVHclientConfigurationPage
    /// </summary>
    class TVHclientConfigurationPage  : IPluginConfigurationPage
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
        public Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream("TVHeadEnd.Configuration.configPage.html");
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "TVHclient"; }
        }

        public IPlugin Plugin
        {
            get { return TVHeadEnd.Plugin.Instance; }
        }
    }
}