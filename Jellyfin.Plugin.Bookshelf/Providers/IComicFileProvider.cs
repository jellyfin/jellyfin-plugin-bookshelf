using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Bookshelf.Providers
{
    public interface IComicFileProvider
    {
        Task<MetadataResult<Book>> ReadMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken);

        bool HasItemChanged(BaseItem item, IDirectoryService directoryService);
    }
}
