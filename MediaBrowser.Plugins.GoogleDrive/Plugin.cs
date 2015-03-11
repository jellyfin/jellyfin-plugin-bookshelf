using System;
using System.Linq;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.GoogleDrive.Configuration;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public static Plugin Instance { get; private set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name
        {
            get { return Constants.Name; }
        }

        public override string Description
        {
            get { return Constants.Description; }
        }

        public override void UpdateConfiguration(BasePluginConfiguration configuration)
        {
            var pluginConfiguration = (PluginConfiguration)configuration;

            foreach (var user in pluginConfiguration.Users)
            {
                user.Id = Guid.NewGuid().ToString();
            }

            base.UpdateConfiguration(configuration);
        }
    }
}
