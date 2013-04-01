using MediaBrowser.Model.Plugins;
//change to test commit
namespace MediaBrowser.Plugins.Dlna.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets the friendly name of the DLNA Server.
        /// </summary>
        /// <value>The friendly name of the DLNA Server.</value>
        public string FriendlyDlnaName { get; set; }

        /// <summary>
        /// Gets or sets the Port Number for the DLNA Server.
        /// </summary>
        /// <value>The Port Number of the DLNA Server.</value>
        public short? DlnaPortNumber { get; set; }

        /// <summary>
        /// Gets or sets the name of the user to impersonate.
        /// </summary>
        /// <value>The name of the User.</value>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the way video uris is put together, here to make testing iterations much quicker.
        /// </summary>
        /// <value>The format string to use for the uri.</value>
        public string VideoCustomUriFormatString { get; set; }
        /// <summary>
        /// Gets or sets the way video uris is put together, here to make testing iterations much quicker.
        /// </summary>
        /// <value>The format string to use for the uri.</value>
        public string VideoCustomProtocolInfoString { get; set; }
        /// <summary>
        /// Gets or sets the way video uris is put together, here to make testing iterations much quicker.
        /// </summary>
        /// <value>The format string to use for the uri.</value>
        public string VideoExtensionsToServeCustom { get; set; }

        /// <summary>
        /// Gets or sets the way video uris is put together, here to make testing iterations much quicker.
        /// </summary>
        /// <value>The format string to use for the uri.</value>
        public string VideoExtensionsToServeStatic { get; set; }

        /// <summary>
        /// Gets or sets the way video uris is put together, here to make testing iterations much quicker.
        /// </summary>
        /// <value>The format string to use for the uri.</value>
        public string VideoExtensionsToServeWithDefaultTranscoding { get; set; }


        /// <summary>
        /// Gets or sets the way Audio uris is put together, here to make testing iterations much quicker.
        /// </summary>
        /// <value>The format string to use for the uri.</value>
        public string AudioUriFormatString { get; set; }

        /// <summary>
        /// Gets or sets the Mime Type to report for Audio, here to make testing iterations much quicker.
        /// </summary>
        /// <value>The Mime Type used for Audio.</value>
        public string AudioMimeType { get; set; }




        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public PluginConfiguration()
            : base()
        {
            //this.DlnaPortNumber = 1845;
            this.FriendlyDlnaName = "MB3 UPnP";
            this.UserName = string.Empty;
            //this.VideoMimeType = "video/x-ms-asf";
            //this.VideoUriFormatString = "{0}Videos/{1}/stream.asf?audioChannels=2&audioBitrate=128000&videoBitrate=5000000&maxWidth=1920&maxHeight=1080&videoCodec=h264&audioCodec=aac";

            this.VideoCustomUriFormatString = "{0}Videos/{1}/stream.asf?audioChannels=2&audioBitrate=128000&videoBitrate=5000000&maxWidth=1920&maxHeight=1080&videoCodec=h264&audioCodec=aac";
            this.VideoCustomProtocolInfoString = "{0}Videos/{1}/stream.asf?audioChannels=2&audioBitrate=128000&videoBitrate=5000000&maxWidth=1920&maxHeight=1080&videoCodec=h264&audioCodec=aac";
            this.VideoExtensionsToServeCustom = ".mkv, .mpeg, .avi, .asf, .wmv, .mp4, .m2ts, .ts, .ogv, .m4v, .webm";
            this.VideoExtensionsToServeStatic = ".mkv, .mpeg, .avi, .asf, .wmv, .mp4, .m2ts, .ts, .ogv, .m4v, .webm";
            this.VideoExtensionsToServeWithDefaultTranscoding = ".mkv, .mpeg, .avi, .asf, .wmv, .mp4, .m2ts, .ts, .ogv, .m4v, .webm";
            this.AudioMimeType = "audio/mpeg";
            this.AudioUriFormatString = "{0}Audio/{1}/stream.mp3";
        }
    }
}
