using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.XbmcMetadata.Parsers;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MediaBrowser.Plugins.XbmcMetadata.Providers
{
    public class EpisodeNfoProvider : BaseNfoProvider<Episode>
    {
        private readonly ILogger _logger;

        public EpisodeNfoProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(LocalMetadataResult<Episode> result, string path, CancellationToken cancellationToken)
        {
            var images = new List<LocalImageInfo>();
            var chapters = new List<ChapterInfo>();

            new EpisodeNfoParser(_logger).Fetch(result.Item, images, chapters, path, cancellationToken);

            result.Images = images;
            result.Chapters = chapters;
        }

        protected override FileSystemInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            var path = Path.ChangeExtension(info.Path, ".nfo");

            return directoryService.GetFile(path);
        }
    }
}
