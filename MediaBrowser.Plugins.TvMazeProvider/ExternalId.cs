using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Providers.TV.TvMaze;

namespace MediaBrowser.Plugins.TvMazeProvider
{
    public class TvMazeExternalId : IExternalId
    {
        public string Name
        {
            get { return "TV Maze Series"; }
        }

        public string Key
        {
            get { return MetadataProviders.TvMaze.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "http://www.tvmaze.com/shows/{0}"; }
        }

        public bool Supports(Model.Entities.IHasProviderIds item)
        {
            return item is Series;
        }
    }

    public class TvMazeEpisodeExternalId : IExternalId
    {
        public string Name
        {
            get { return "TV Maze Episode"; }
        }

        public string Key
        {
            get { return MetadataProviders.TvMaze.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "http://www.tvmaze.com/episodes/{0}"; }
        }

        public bool Supports(Model.Entities.IHasProviderIds item)
        {
            return item is Episode;
        }
    }

    public class TvMazeSeasonExternalId : IExternalId
    {
        public string Name
        {
            get { return "TV Maze Season"; }
        }

        public string Key
        {
            get { return MetadataProviders.TvMaze.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "http://www.tvmaze.com/seasons/{0}/season"; }
        }

        public bool Supports(Model.Entities.IHasProviderIds item)
        {
            return item is Season;
        }
    }
}
