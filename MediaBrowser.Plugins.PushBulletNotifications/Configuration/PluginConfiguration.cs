using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.PushBulletNotifications.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PushBulletOptions[] Options { get; set; }

        public PluginConfiguration()
        {
            Options = new PushBulletOptions[] { };
        }
    }

    public class PushBulletOptions
    {
        public Boolean Enabled { get; set; }
        public String Token { get; set; }
        public String DeviceId { get; set; }
        public string MediaBrowserUserId { get; set; }
    }

}
