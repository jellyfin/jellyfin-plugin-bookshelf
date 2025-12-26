using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Bookshelf.Providers;

/// <summary>
/// Comic file provider.
/// </summary>
public class ComicFileProvider : ILocalMetadataProvider<Book>, IHasItemChangeMonitor
{
    private readonly IEnumerable<IComicFileProvider> _comicFileProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComicFileProvider"/> class.
    /// </summary>
    /// <param name="comicFileProviders">The list of comic file providers.</param>
    public ComicFileProvider(IEnumerable<IComicFileProvider> comicFileProviders)
    {
        _comicFileProviders = comicFileProviders;
    }

    /// <inheritdoc />
    public string Name => "Comic Provider";

    /// <inheritdoc />
    public async Task<MetadataResult<Book>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
    {
        foreach (IComicFileProvider iComicFileProvider in _comicFileProviders)
        {
            var metadata = await iComicFileProvider.ReadMetadata(info, directoryService, cancellationToken)
                .ConfigureAwait(false);

            if (metadata.HasMetadata)
            {
                return metadata;
            }
        }

        return new MetadataResult<Book> { HasMetadata = false };
    }

    /// <inheritdoc />
    public bool HasChanged(BaseItem item, IDirectoryService directoryService)
    {
        foreach (IComicFileProvider iComicFileProvider in _comicFileProviders)
        {
            var fileChanged = iComicFileProvider.HasItemChanged(item);

            if (fileChanged)
            {
                return fileChanged;
            }
        }

        return false;
    }
}
