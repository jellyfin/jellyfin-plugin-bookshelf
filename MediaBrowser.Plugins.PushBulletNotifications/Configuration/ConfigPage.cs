using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using System.IO;

namespace MediaBrowser.Plugins.PushBulletNotifications.Configuration
{
    class ConfigPage : IPluginConfigurationPage
    {
        public string Name
        {
            get { return Plugin.Name; }
        }

        public Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream(GetType().Namespace + ".config.html");
        }

        public ConfigurationPageType ConfigurationPageType
        {
            get { return ConfigurationPageType.PluginConfiguration; }
        }

        public IPlugin Plugin
        {
            get { return PushBulletNotifications.Plugin.Instance; }
        }
    }
}
