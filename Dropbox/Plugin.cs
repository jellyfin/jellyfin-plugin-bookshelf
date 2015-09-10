using Dropbox.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace Dropbox
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
    }
}
