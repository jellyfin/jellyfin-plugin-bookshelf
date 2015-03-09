using FolderSync.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using System.Threading;

namespace FolderSync
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public SemaphoreSlim TraktResourcePool = new SemaphoreSlim(2,2);

        public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer)
            : base(appPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name
        {
            get { return StaticName; }
        }

        public static string StaticName
        {
            get { return "Folder Sync"; }
        }


        public override string Description
        {
            get
            {
                return "Syncs media to folders";
            }
        }

        public static Plugin Instance { get; private set; }
        
        public PluginConfiguration PluginConfiguration
        {
            get { return Configuration; }
        }
    }
}
