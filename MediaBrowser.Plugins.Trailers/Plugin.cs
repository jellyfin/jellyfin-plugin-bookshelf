using System.IO;
using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.Trailers.Configuration;

namespace MediaBrowser.Plugins.Trailers
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        /// <summary>
        /// Apple doesn't seem to like too many simulataneous requests.
        /// </summary>
        public readonly SemaphoreSlim AppleTrailerVideos = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The apple trailer images
        /// </summary>
        public readonly SemaphoreSlim AppleTrailerImages = new SemaphoreSlim(1, 1);

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// The _download path
        /// </summary>
        private string _downloadPath;
        /// <summary>
        /// Gets the path to the trailer download directory
        /// </summary>
        /// <value>The download path.</value>
        public string DownloadPath
        {
            get
            {
                if (_downloadPath == null)
                {
                    // Use 
                    _downloadPath = Configuration.DownloadPath;

                    if (string.IsNullOrWhiteSpace(_downloadPath))
                    {
                        _downloadPath = Path.Combine(ApplicationPaths.DataPath, Name);
                    }

                    if (!Directory.Exists(_downloadPath))
                    {
                        Directory.CreateDirectory(_downloadPath);
                    }
                }
                return _downloadPath;
            }
            set
            {
                _downloadPath = value;
            }
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Trailers"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "Movie trailers for your collection.";
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }

        /// <summary>
        /// Updates the configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public override void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            var oldConfig = Configuration;

            base.UpdateConfiguration(configuration);

            ServerEntryPoint.Instance.OnConfigurationUpdated(oldConfig, (PluginConfiguration)configuration);
        }
    }
}
