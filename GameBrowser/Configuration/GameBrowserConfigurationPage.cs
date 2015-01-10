using System.ComponentModel.Composition;
using System.IO;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;

namespace GameBrowser.Configuration
{

    class GameBrowserConfigurationPage : IPluginConfigurationPage
    {
        public string Name
        {
            get { return "GameBrowser"; }
        }



        public Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream("GameBrowser.Configuration.configPage.html");
        }



        public IPlugin Plugin
        {
            get { return GameBrowser.Plugin.Instance; }
        }



        public ConfigurationPageType ConfigurationPageType
        {
            get { return ConfigurationPageType.PluginConfiguration; }
        }
    }
}
