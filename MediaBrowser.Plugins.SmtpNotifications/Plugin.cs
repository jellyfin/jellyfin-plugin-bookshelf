using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.SmtpNotifications.Configuration;

namespace MediaBrowser.Plugins.SmtpNotifications
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        protected ILogger Logger { get; set; }
        private readonly IEncryptionManager _encryption;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogManager logManager, IEncryptionManager encryption)
            : base(applicationPaths, xmlSerializer)
        {
            _encryption = encryption;
            Instance = this;
            Logger = logManager.GetLogger("SMTP Notifications");
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
                }
            };
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
                optionSet.PwData = _encryption.EncryptString(optionSet.Password ?? string.Empty);
                optionSet.Password = null;
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
