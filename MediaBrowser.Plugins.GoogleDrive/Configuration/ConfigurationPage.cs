using System.IO;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;

namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public class ConfigurationPage : IPluginConfigurationPage
    {
        public string Name
        {
            get { return Constants.Name.Replace(" ", ""); }
        }

        public ConfigurationPageType ConfigurationPageType
        {
            get { return ConfigurationPageType.PluginConfiguration; }
        }

        public IPlugin Plugin
        {
            get { return GoogleDrive.Plugin.Instance; }
        }

        public Stream GetHtmlStream()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".configPage.html");
        }
    }
}
