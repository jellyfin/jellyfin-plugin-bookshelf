using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.Vimeo.Configuration;
using MediaBrowser.Plugins.Vimeo.VimeoAPI.API;

namespace MediaBrowser.Plugins.Vimeo
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public static VimeoClient vc;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            vc = new VimeoClient("b3f7452b9822b91cede55a3315bee7e021c876c0", "eb62bfd0a204c316a4f05b1d3a9d88726718a893");

            if (Instance.Configuration.Token != "" && Instance.Configuration.SecretToken != "")
            {
                vc.Token = Instance.Configuration.Token;
                vc.TokenSecret = Instance.Configuration.SecretToken;
                vc.Login();
            }
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Vimeo"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "Vimeo videos for your collection.";
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }
    }
}
