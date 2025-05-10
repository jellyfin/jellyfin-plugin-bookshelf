using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Bookshelf.Providers;

/// <summary>
/// External url provider for ISBN.
/// </summary>
public class ISBNExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => "ISBN";

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item.TryGetProviderId("ISBN", out var externalId))
        {
            if (item is Book)
            {
                yield return $"https://search.worldcat.org/search?q=bn:{externalId}";
            }
        }
    }
}
