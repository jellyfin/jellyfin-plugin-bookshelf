using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.ProwlNotifications.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public ProwlOptions[] Options { get; set; }

        public PluginConfiguration()
        {
            Options = new ProwlOptions[] { };
        }
    }

    public class ProwlOptions
    {
        public Boolean Enabled { get; set; }
        public String Token { get; set; }
        public string MediaBrowserUserId { get; set; }
    }
}
