using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.StudioCleaner
{
    /// <summary>
    /// Class ServerEntryPoint
    /// </summary>
    public class ServerEntryPoint : IRequiresRegistration
    {
        /// <summary>
        /// Access to the LibraryManager of MB Server
        /// </summary>
        public ILibraryManager LibraryManager { get; private set; }

        /// <summary>
        /// Access to the SecurityManager of MB Server
        /// </summary>
        public ISecurityManager PluginSecurityManager { get; set; }

        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerEntryPoint" /> class.
        /// </summary>
        public ServerEntryPoint(ILibraryManager libraryManager, ILogManager logManager, ISecurityManager securityManager)
        {
            LibraryManager = libraryManager;
            PluginSecurityManager = securityManager;
            _logger = logManager.GetLogger(Plugin.Instance.Name);
        }

        /// <summary>
        /// Loads our registration information
        ///
        /// </summary>
        /// <returns></returns>
        public async Task LoadRegistrationInfoAsync()
        {
            Plugin.Instance.Registration = await PluginSecurityManager.GetRegistrationStatus("StudioCleaner").ConfigureAwait(false);
            _logger.Debug("StudioCleaner Registration Status - Registered: {0} In trial: {2} Expiration Date: {1} Is Valid: {3}", Plugin.Instance.Registration.IsRegistered, Plugin.Instance.Registration.ExpirationDate, Plugin.Instance.Registration.TrialVersion, Plugin.Instance.Registration.IsValid);
        }
    }
}
