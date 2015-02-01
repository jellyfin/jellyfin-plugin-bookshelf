using MediaBrowser.Model.Plugins;

namespace RokuMetadata.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool EnableExtractionDuringLibraryScan { get; set; }
        public bool EnableHdThumbnails { get; set; }
        public bool EnableSdThumbnails { get; set; }
        public int MaxBitrate { get; set; }

        public AudioOutputMode AudioOutputMode { get; set; }

        public PluginConfiguration()
        {
            EnableHdThumbnails = true;
            MaxBitrate = 20000000;
            AudioOutputMode = AudioOutputMode.DTS;
        }
    }

    public enum AudioOutputMode
    {
        Stereo = 0,
        DD = 1,
        DDPlus = 2,
        DTS = 3
    }
}
