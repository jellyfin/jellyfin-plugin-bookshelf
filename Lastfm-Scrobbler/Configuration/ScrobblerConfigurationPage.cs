namespace LastfmScrobbler.Configuration
{
    using MediaBrowser.Common.Plugins;
    using MediaBrowser.Controller.Plugins;
    using System.IO;

    /// <summary>
    /// Class MyConfigurationPage
    /// </summary>
    class ScrobblerConfigurationPage : IPluginConfigurationPage
    {
        /// <summary>
        /// Gets My Option.
        /// </summary>
        /// <value>The Option.</value>
        public string Name
        {
            get { return "Last.fm Scrobbler"; }
        }

        /// <summary>
        /// Gets the HTML stream.
        /// </summary>
        /// <returns>Stream.</returns>
        public Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream("LastfmScrobbler.Configuration.configPage.html");
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
            get { return LastfmScrobbler.Plugin.Instance; }
        }
    }
}
