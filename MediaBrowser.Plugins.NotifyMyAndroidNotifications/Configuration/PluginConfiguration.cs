using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.NotifyMyAndroidNotifications.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public NotifyMyAndroidOptions[] Options { get; set; }

        public PluginConfiguration()
        {
            Options = new NotifyMyAndroidOptions[] { };
        }
    }

    public class NotifyMyAndroidOptions
    {
        public Boolean Enabled { get; set; }
        public String Token { get; set; }
        public string MediaBrowserUserId { get; set; }
    }
}
