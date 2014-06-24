using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.XbmcMetadata.Parsers;
using System.IO;
using System.Threading;

namespace MediaBrowser.Plugins.XbmcMetadata.Providers
{
    public class SeriesNfoProvider : BaseNfoProvider<Series>
    {
        private readonly ILogger _logger;

        public SeriesNfoProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(LocalMetadataResult<Series> result, string path, CancellationToken cancellationToken)
        {
            new SeriesNfoParser(_logger).Fetch(result.Item, path, cancellationToken);
        }

        protected override FileSystemInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "series.nfo"));
        }
    }
}
