using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

#nullable enable
namespace Jellyfin.Plugin.Bookshelf.Providers
{
    public class ComicFileProvider : ILocalMetadataProvider<Book>, IHasItemChangeMonitor
    {
        protected readonly ILogger<ComicFileProvider> _logger;

        private readonly IFileSystem _fileSystem;
        private readonly IEnumerable<IComicFileProvider> _iComicFileProviders;

        public string Name => "Comic Provider";

        public ComicFileProvider(IFileSystem fileSystem, ILogger<ComicFileProvider> logger, IEnumerable<IComicFileProvider> iComicFileProviders)
        {
            this._fileSystem = fileSystem;
            this._logger = logger;

            this._iComicFileProviders = iComicFileProviders;
        }

        public async Task<MetadataResult<Book>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            foreach (IComicFileProvider iComicFileProvider in _iComicFileProviders)
            {
                var metadata = await iComicFileProvider.ReadMetadata(info, directoryService, cancellationToken);

                if (metadata.HasMetadata)
                {
                    return metadata;
                }
            }
            return new MetadataResult<Book> { HasMetadata = false };
        }

        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            foreach (IComicFileProvider iComicFileProvider in _iComicFileProviders)
            {
                var fileChanged = iComicFileProvider.HasItemChanged(item, directoryService);

                if (fileChanged)
                {
                    return fileChanged;
                }
            }
            return false;
        }
    }
}
