using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Bookshelf.Providers
{
    /// <summary>
    /// Comic file provider interface.
    /// </summary>
    public interface IComicFileProvider
    {
        /// <summary>
        /// Read the item metadata.
        /// </summary>
        /// <param name="info">The item info.</param>
        /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The metadata result.</returns>
        ValueTask<MetadataResult<Book>> ReadMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken);

        /// <summary>
        /// Has the item changed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Item change status.</returns>
        bool HasItemChanged(BaseItem item);
    }
}
