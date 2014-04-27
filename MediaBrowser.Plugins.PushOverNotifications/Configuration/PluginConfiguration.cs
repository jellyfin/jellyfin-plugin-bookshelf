using System;
using System.Collections.Generic;
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
        public List<Sound> SoundList { get; set; }
        public int Priority { get; set; }
        public string MediaBrowserUserId { get; set; }

        public PushOverOptions()
        {
            SoundList = new List<Sound>
            {
                new Sound() {Name = "Pushover", Value = "pushover"},
                new Sound() {Name = "Bike", Value = "bike"},
                new Sound() {Name = "Bugle", Value = "bugle"}
            };
        }
    }

    public class Sound
    {
        public String Name { get; set; }
        public String Value { get; set; }
    }
}
