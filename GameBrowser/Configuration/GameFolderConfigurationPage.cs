using System.IO;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;

namespace GameBrowser.Configuration
{
    class GameFolderConfigurationPage : IPluginConfigurationPage
    {

        public string Name
        {
            get { return "GameFolderConfigurationPage"; }
        }



        public Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream("GameBrowser.Configuration.GameFolderConfig.html");
        }



        public IPlugin Plugin
        {
            get { return GameBrowser.Plugin.Instance; }
        }



        public ConfigurationPageType ConfigurationPageType
        {
            get { return ConfigurationPageType.None; }
        }
    }
}
