using System.Security.Cryptography;
using System.Text;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.SmtpNotifications.Configuration;

namespace MediaBrowser.Plugins.SmtpNotifications
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        protected ILogger Logger { get; set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogManager logManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            Logger = logManager.GetLogger("SMTP Notifications");
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Email Notifications"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "Sends notifications via email.";
            }
        }

        public override void UpdateConfiguration(Model.Plugins.BasePluginConfiguration configuration)
        {
            var config = (PluginConfiguration) configuration;

            // Encrypt password for saving.  The Password field the config page sees will always be blank except when updated.
            // The program actually uses the encrypted version
            foreach (var optionSet in config.Options)
            {
                try
                {
                    optionSet.PwData = Encoding.Default.GetString(ProtectedData.Protect(Encoding.Default.GetBytes(optionSet.Password ?? ""), null, DataProtectionScope.LocalMachine));
                    optionSet.Password = null;
                }
                catch (CryptographicException e)
                {
                    Logger.ErrorException("Error saving password", e);
                }
            }

            base.UpdateConfiguration(configuration);
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }
    }
}
