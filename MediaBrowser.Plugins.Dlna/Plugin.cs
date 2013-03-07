using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.Dlna.Configuration;

namespace MediaBrowser.Plugins.Dlna
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "DLNA Server"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get { return "DLNA Server"; }
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
            base.UpdateConfiguration(configuration);

            ServerEntryPoint.Instance.CleanupUPnPServer();
            ServerEntryPoint.Instance.SetupUPnPServer();

            //this is temporary code so that testers can try various combinations with their devices without needing a recompile all the time
            Model.VideoItemPlatinumMediaResourceHelper.MimeType = Plugin.Instance.Configuration.VideoMimeType;
            Model.VideoItemPlatinumMediaResourceHelper.UriFormatString = Plugin.Instance.Configuration.VideoUriFormatString;
            Model.MusicItemPlatinumMediaResourceHelper.MimeType = Plugin.Instance.Configuration.AudioMimeType;
            Model.MusicItemPlatinumMediaResourceHelper.UriFormatString = Plugin.Instance.Configuration.AudioUriFormatString;
        }
    }
}
