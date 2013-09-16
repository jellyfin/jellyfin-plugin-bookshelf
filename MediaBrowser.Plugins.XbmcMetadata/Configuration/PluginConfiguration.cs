using MediaBrowser.Model.Plugins;
using System;

namespace MediaBrowser.Plugins.XbmcMetadata.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public Guid? UserId { get; set; }
    }
}
