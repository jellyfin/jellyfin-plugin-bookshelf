using System.ComponentModel.Composition;
using System.IO;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;

namespace GameBrowser.Configuration
{
    class GbMetaConfigurationPage : IPluginConfigurationPage
    {

        public string Name
        {
            get { return "GbMetaConfigurationPage"; }
        }



        public Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream("GameBrowser.Configuration.metaConfig.html");
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
