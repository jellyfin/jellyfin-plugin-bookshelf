using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.Trailers.Configuration;
using MediaBrowser.Plugins.Trailers.Entities;
using MediaBrowser.Plugins.Trailers.ScheduledTasks;
using System.Linq;
using System.Threading;

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
        public readonly SemaphoreSlim AppleTrailers = new SemaphoreSlim(1, 1);

        private readonly ITaskManager _taskManager;
        private readonly ILibraryManager _libraryManager;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ITaskManager taskManager, ILibraryManager libraryManager)
            : base(applicationPaths, xmlSerializer)
        {
            _taskManager = taskManager;
            _libraryManager = libraryManager;
            Instance = this;
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

        public override async void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            var config = (PluginConfiguration) configuration;

            var hdChanged = config.EnableHDTrailers != Configuration.EnableHDTrailers;

            base.UpdateConfiguration(configuration);

            if (hdChanged)
            {
                _taskManager.CancelIfRunning<CurrentTrailerDownloadTask>();
                
                var folder = _libraryManager.RootFolder.Children
                    .OfType<TrailerCollectionFolder>()
                    .FirstOrDefault();

                if (folder != null)
                {
                    await folder.ClearChildren(CancellationToken.None).ConfigureAwait(false);
                }

                _taskManager.CancelIfRunningAndQueue<CurrentTrailerDownloadTask>();
            }
        }
    }
}
