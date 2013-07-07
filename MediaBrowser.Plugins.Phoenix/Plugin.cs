using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.Phoenix.Configuration;
using MediaBrowser.Theater.Interfaces.Plugins;
using System;

namespace MediaBrowser.Plugins.Phoenix
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasThumbImage
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Phoenix"; }
        }

        public Uri ThumbUri
        {
            get { return new Uri("pack://application:,,,/MediaBrowser.Plugins.Phoenix;component/Resources/tile.jpg", UriKind.Absolute); }
        }
    }
}
