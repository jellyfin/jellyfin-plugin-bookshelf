using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using System.IO;

namespace Trakt.Configuration
{
    /// <summary>
    /// Class TraktConfigurationPage
    /// </summary>
    class TraktConfigurationPage : IPluginConfigurationPage
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name => "Trakt for MediaBrowser";

        /// <summary>
        /// Gets the HTML stream.
        /// </summary>
        /// <returns>Stream.</returns>
        public Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream(GetType().Namespace + ".configPage.html");
        }

        /// <summary>
        /// Gets the type of the configuration page.
        /// </summary>
        /// <value>The type of the configuration page.</value>
        public ConfigurationPageType ConfigurationPageType => ConfigurationPageType.PluginConfiguration;

        public IPlugin Plugin => Trakt.Plugin.Instance;
    }
}
