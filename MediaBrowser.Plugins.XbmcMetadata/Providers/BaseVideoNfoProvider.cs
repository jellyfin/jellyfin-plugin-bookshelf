using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.XbmcMetadata.Parsers;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MediaBrowser.Plugins.XbmcMetadata.Providers
{
    public class BaseVideoNfoProvider<T> : BaseNfoProvider<T>
        where T : Video, new ()
    {
        private readonly ILogger _logger;

        public BaseVideoNfoProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(LocalMetadataResult<T> result, string path, CancellationToken cancellationToken)
        {
            var chapters = new List<ChapterInfo>();

            new MovieNfoParser(_logger).Fetch(result.Item, chapters, path, cancellationToken);

            result.Chapters = chapters;
        }

        protected override FileSystemInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            var path = GetMovieSavePath(info);

            return directoryService.GetFile(path);
        }

        public static string GetMovieSavePath(ItemInfo item)
        {
            if (Directory.Exists(item.Path))
            {
                var path = item.Path;

                return Path.Combine(path, Path.GetFileNameWithoutExtension(path) + ".nfo");
            }

            return Path.ChangeExtension(item.Path, ".nfo");
        }
    }
}