using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using PodCasts.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace PodCasts
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public static ILogger Logger { get; set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Podcasts"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "Podcasts for your collection.";
            }
        }

        //public string DownloadFolder
        //{
        //    get
        //    {
        //        if (Configuration.PodCastFolder == null)
        //        {
        //            Configuration.PodCastFolder = Path.Combine(ServerEntryPoint.Instance.ApplicationPaths.DataPath, "PodCasts");
        //            if (!Directory.Exists(Configuration.PodCastFolder)) Directory.CreateDirectory(Configuration.PodCastFolder);
        //            SaveConfiguration();
        //        }

        //        return Configuration.PodCastFolder;
        //    }
        //}

        public static readonly SemaphoreSlim ResourcePool = new SemaphoreSlim(3, 3);

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }

        /// <summary>
        /// Holds our registration information
        /// </summary>
        public MBRegistrationRecord Registration { get; set; }

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
