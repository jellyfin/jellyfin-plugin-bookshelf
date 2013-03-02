using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using System;

namespace MediaBrowser.Plugins.MpcHc
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<BasePluginConfiguration>, IUIPlugin
    {
        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "MPC-HC Integration"; }
        }

        /// <summary>
        /// Gets the minimum required UI version.
        /// </summary>
        /// <value>The minimum required UI version.</value>
        public Version MinimumRequiredUIVersion
        {
            get { return new Version("2.9.4782.23738"); }
        }
    }
}
