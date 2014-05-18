using System;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.Vimeo.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {

        public String Username { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public PluginConfiguration()
            : base()
        {
            
        }
    }
}
