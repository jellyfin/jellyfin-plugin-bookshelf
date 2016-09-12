using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using Trakt.Configuration;

namespace Trakt
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public SemaphoreSlim TraktResourcePool = new SemaphoreSlim(2, 2);

        public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer)
            : base(appPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name => "Trakt";


        public override string Description
            => "Watch, rate and discover media using Trakt. The htpc just got more social";

        public static Plugin Instance { get; private set; }

        public PluginConfiguration PluginConfiguration => Configuration;
    }
}
