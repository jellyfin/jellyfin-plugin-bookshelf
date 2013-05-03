using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Plugins.Trailers.ScheduledTasks;

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
        /// The _task manager
        /// </summary>
        private readonly ITaskManager _taskManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerEntryPoint" /> class.
        /// </summary>
        /// <param name="taskManager">The task manager.</param>
        public ServerEntryPoint(ITaskManager taskManager)
        {
            _taskManager = taskManager;

            Instance = this;
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
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
