using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.LocalTrailers.Configuration;
using System.Threading;

namespace MediaBrowser.Plugins.LocalTrailers
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        /// <summary>
        /// Apple doesn't seem to like too many simulataneous requests.
        /// </summary>
        public readonly SemaphoreSlim AppleTrailers = new SemaphoreSlim(1, 1);

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
            get { return "Local Trailers"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "Download trailers for your collection.";
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }
    }
}
