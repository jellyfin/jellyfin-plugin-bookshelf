using System.Collections.Generic;
using System.Threading;
using Jellyfin.Plugin.Bookshelf.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Bookshelf
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public readonly SemaphoreSlim GoogleBooksSemiphore = new SemaphoreSlim(5, 5);
        public readonly SemaphoreSlim ComicVineSemiphore = new SemaphoreSlim(5, 5);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationPaths"></param>
        /// <param name="xmlSerializer"></param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// 
        /// </summary>
        public override string Name
        {
            get { return "MB Bookshelf"; }
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
                }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public static Plugin Instance { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public PluginConfiguration PluginConfiguration
        {
            get { return Configuration; }
        }
    }
}
