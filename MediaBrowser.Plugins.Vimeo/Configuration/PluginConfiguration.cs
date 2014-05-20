using System;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.Vimeo.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {

        public String AccessCode { get; set; }
        public String Token { get; set; }
        public String SecretToken { get; set; }
        public String TokenURL { get; set; }
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
