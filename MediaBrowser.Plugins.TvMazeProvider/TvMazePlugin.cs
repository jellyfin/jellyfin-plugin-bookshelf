using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.TvMazeProvider
{
    public class TvMazePlugin : BasePlugin<TvMazePluginConfiguration>
    {
        public TvMazePlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
        }

        public override string Name
        {
            get { return "TV Maze Provider"; }
        }

        public override string Description
        {
            get { return "Metadata provider for TV shows using data from www.tvmaze.com"; }
        }
    }

    public class TvMazePluginConfiguration : BasePluginConfiguration
    {
    }
}
