using System;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Plugins.NextPvr.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string WebServiceUrl { get; set; }
        public string Pin { get; set; }
        public bool TimeShift { get; set; }
        public Boolean EnableDebugLogging { get; set; }

        public PluginConfiguration()
        {
            Pin = "0000";
            WebServiceUrl = "http://localhost:8866";
            TimeShift = false;
            EnableDebugLogging = false;
        }
    }
}
