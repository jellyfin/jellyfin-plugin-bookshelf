using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.ADEProvider
{
    public class ADEPlugin : BasePlugin<ADEPluginConfiguration>
    {
        public ADEPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
        }

        public override string Name
        {
            get { return "ADE Provider"; }
        }

        public override string Description
        {
            get { return "Gets metadata for adult videos from AdultDVDEmpire.com"; }
        }
    }

    public class ADEPluginConfiguration : BasePluginConfiguration
    {
    }
}
