using System;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.PushALotNotifications.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PushALotOptions[] Options { get; set; }

        public PluginConfiguration()
        {
            Options = new PushALotOptions[] { };
        }
    }

    public class PushALotOptions
    {
        public Boolean Enabled { get; set; }
        public String Token { get; set; }

        public string MediaBrowserUserId { get; set; }
    }
}
