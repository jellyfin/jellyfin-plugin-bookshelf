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

        public string ReleaseDateFormat { get; set; }

        public bool SaveImagePathsInNfo { get; set; }
        public bool EnablePathSubstitution { get; set; }

        public PluginConfiguration()
        {
            ReleaseDateFormat = "yyyy-MM-dd";

            SaveImagePathsInNfo = true;
            EnablePathSubstitution = true;
        }
    }
}
