using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Entities;
using PodCasts.Configuration;
using MediaBrowser.Model.Logging;
using PodCasts.Tasks;

namespace PodCasts
{
    /// <summary>
    /// Class ServerEntryPoint
    /// </summary>
    public class ServerEntryPoint : IServerEntryPoint, IRequiresRegistration
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

        public ILibraryManager LibraryManager { get; private set; }
        public IConfigurationManager ConfigurationManager { get; set; }
        public IApplicationPaths ApplicationPaths { get; set; }
        public ISecurityManager PluginSecurityManager { get; set; }
        public IItemRepository ItemRepository { get; set; }
        public INotificationManager NotificationManager { get; set; }
        public IUserManager UserManager { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerEntryPoint" /> class.
        /// </summary>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="libraryManager"></param>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="logManager"></param>
        /// <param name="applicationPaths"></param>
        /// <param name="configurationManager"></param>
        /// <param name="repo"></param>
        public ServerEntryPoint(ITaskManager taskManager, ILibraryManager libraryManager, INotificationManager notificationManager, ILogManager logManager, ISecurityManager securityManager,
            IApplicationPaths applicationPaths, IServerConfigurationManager configurationManager, IItemRepository repo, IUserManager userManager)
        {
            _taskManager = taskManager;
            LibraryManager = libraryManager;
            ConfigurationManager = configurationManager;
            ApplicationPaths = applicationPaths;
            ItemRepository = repo;
            NotificationManager = notificationManager;
            UserManager = userManager;
            PluginSecurityManager = securityManager;
            Plugin.Logger = logManager.GetLogger(Plugin.Instance.Name);

            Instance = this;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            if (Plugin.Instance.Configuration.Feeds.Count > 0 && (DateTime.Now - Plugin.Instance.Configuration.LastFeedUpdate).TotalHours > 24)
            {
                //Run the download task
                _taskManager.QueueScheduledTask<PodCastsDownloadTask>();
            }
        }

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        /// <param name="oldConfig">The old config.</param>
        /// <param name="newConfig">The new config.</param>
        public void OnConfigurationUpdated(PluginConfiguration oldConfig, PluginConfiguration newConfig)
        {
            //Run the download task
            _taskManager.QueueScheduledTask<PodCastsDownloadTask>();

        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Loads our registration information
        ///
        /// </summary>
        /// <returns></returns>
        public async Task LoadRegistrationInfoAsync()
        {
            Plugin.Instance.Registration = await PluginSecurityManager.GetRegistrationStatus("PodCasts").ConfigureAwait(false);
            Plugin.Logger.Debug("PodCasts Registration Status - Registered: {0} In trial: {2} Expiration Date: {1} Is Valid: {3}", Plugin.Instance.Registration.IsRegistered, Plugin.Instance.Registration.ExpirationDate, Plugin.Instance.Registration.TrialVersion, Plugin.Instance.Registration.IsValid);
        }
    }
}
