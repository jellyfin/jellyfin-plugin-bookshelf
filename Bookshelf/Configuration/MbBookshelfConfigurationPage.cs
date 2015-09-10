using System.IO;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;

namespace MBBookshelf.Configuration
{
    class MbBookshelfConfigurationPage : IPluginConfigurationPage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream("MBBookshelf.Configuration.configPage.html");
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get { return "MB Bookshelf"; }
        }

        /// <summary>
        /// 
        /// </summary>
        public ConfigurationPageType ConfigurationPageType
        {
            get { return ConfigurationPageType.PluginConfiguration; }
        }

        /// <summary>
        /// 
        /// </summary>
        public IPlugin Plugin
        {
            get { return MBBookshelf.Plugin.Instance; }
        }
    }
}
