using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.XbmcMetadata.Parsers;
using System.IO;
using System.Threading;

namespace MediaBrowser.Plugins.XbmcMetadata.Providers
{
    public class SeasonNfoProvider : BaseNfoProvider<Season>
    {
        private readonly ILogger _logger;

        public SeasonNfoProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(LocalMetadataResult<Season> result, string path, CancellationToken cancellationToken)
        {
            new SeasonNfoParser(_logger).Fetch(result.Item, path, cancellationToken);
        }

        protected override FileSystemInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "season.nfo"));
        }
    }
}

