using System;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.PushOverNotifications.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PushOverOptions[] Options { get; set; }

        public PluginConfiguration()
        {
            Options = new PushOverOptions[] { };
        }
    }

    public class PushOverOptions
    {
        public Boolean Enabled { get; set; }
        public String UserKey { get; set; }
        public String Token { get; set; }
        public String DeviceName { get; set; }

        public string MediaBrowserUserId { get; set; }
    }
}
