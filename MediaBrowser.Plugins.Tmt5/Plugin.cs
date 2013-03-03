using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using System;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.Tmt5
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<BasePluginConfiguration>, IUIPlugin
    {
        public Plugin(IKernel kernel, IXmlSerializer xmlSerializer) : base(kernel, xmlSerializer)
        {
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "TMT5 Integration"; }
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
