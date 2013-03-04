using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.ScheduledTasks;
using MediaBrowser.Plugins.Trailers.Configuration;
using MediaBrowser.Plugins.Trailers.ScheduledTasks;
using System;
using System.IO;

namespace MediaBrowser.Plugins.Trailers
{
    /// <summary>
    /// Class ServerEntryPoint
    /// </summary>
    public class ServerEntryPoint : IServerEntryPoint
    {
        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static ServerEntryPoint Instance { get; private set; }

        /// <summary>
        /// The _application paths
        /// </summary>
        private readonly IApplicationPaths _applicationPaths;

        /// <summary>
        /// The _task manager
        /// </summary>
        private ITaskManager _taskManager;

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
                    _downloadPath = Plugin.Instance.Configuration.DownloadPath;

                    if (string.IsNullOrWhiteSpace(_downloadPath))
                    {
                        _downloadPath = Path.Combine(_applicationPaths.DataPath, Plugin.Instance.Name);
                    }

                    if (!Directory.Exists(_downloadPath))
                    {
                        Directory.CreateDirectory(_downloadPath);
                    }
                }
                return _downloadPath;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerEntryPoint" /> class.
        /// </summary>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="appPaths">The app paths.</param>
        public ServerEntryPoint(ITaskManager taskManager, IApplicationPaths appPaths)
        {
            _taskManager = taskManager;

            Instance = this;
            _applicationPaths = appPaths;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            if (Plugin.Instance.IsFirstRun)
            {
                _taskManager.QueueScheduledTask<CurrentTrailerDownloadTask>();
            }
        }

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        /// <param name="oldConfig">The old config.</param>
        /// <param name="newConfig">The new config.</param>
        public void OnConfigurationUpdated(PluginConfiguration oldConfig, PluginConfiguration newConfig)
        {
            var pathChanged = !string.Equals(oldConfig.DownloadPath, newConfig.DownloadPath, StringComparison.OrdinalIgnoreCase);

            if (pathChanged)
            {
                _downloadPath = null;
                _taskManager.QueueScheduledTask<RefreshMediaLibraryTask>();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
