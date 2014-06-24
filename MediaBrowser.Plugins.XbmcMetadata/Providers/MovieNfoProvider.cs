using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Plugins.XbmcMetadata.Providers
{
    public class MovieNfoProvider : BaseVideoNfoProvider<Movie>
    {
        public MovieNfoProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem, logger)
        {
        }
    }

    public class MusicVideoNfoProvider : BaseVideoNfoProvider<MusicVideo>
    {
        public MusicVideoNfoProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem, logger)
        {
        }
    }

    public class AdultVideoNfoProvider : BaseVideoNfoProvider<AdultVideo>
    {
        public AdultVideoNfoProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem, logger)
        {
        }
    }

    public class VideoNfoProvider : BaseVideoNfoProvider<Video>
    {
        public VideoNfoProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem, logger)
        {
        }
    }

}