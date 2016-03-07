namespace LastfmScrobbler
{
    using Configuration;
    using MediaBrowser.Common.Configuration;
    using MediaBrowser.Common.Plugins;
    using MediaBrowser.Model.Logging;
    using MediaBrowser.Model.Plugins;
    using MediaBrowser.Model.Serialization;
    using System.Threading;

    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        /// <summary>
        /// Flag set when an Import Syncing task is running
        /// </summary>
        public static bool Syncing { get; internal set; }
        
        internal static readonly SemaphoreSlim LastfmResourcePool = new SemaphoreSlim(4, 4);

        //Global logging instance
        public static ILogger Logger { get; set; }

        public PluginConfiguration PluginConfiguration
        {
            get { return Configuration; }
        }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Last.fm Scrobbler"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "Scrobble your music collection to Last.fm";
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }

        /// <summary>
        /// Updates the configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public override void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            var oldConfig = Configuration;

            base.UpdateConfiguration(configuration);

            ServerEntryPoint.Instance.OnConfigurationUpdated(oldConfig, (PluginConfiguration)configuration);
        }
    }
}
