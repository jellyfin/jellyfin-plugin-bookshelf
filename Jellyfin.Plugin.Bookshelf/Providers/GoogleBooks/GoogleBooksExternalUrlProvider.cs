using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks;

/// <summary>
/// External url provider for Google books.
/// </summary>
public class GoogleBooksExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc />
    public string Name => GoogleBooksConstants.ProviderName;

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item.TryGetProviderId(GoogleBooksConstants.ProviderId, out var externalId))
        {
            if (item is Book)
            {
                yield return $"https://books.google.com/books?id={externalId}";
            }
        }
    }
}
