using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine;

/// <summary>
/// Url provider for ComicVine.
/// </summary>
public class ComicVineExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => ComicVineConstants.ProviderName;

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item.TryGetProviderId(ComicVineConstants.ProviderId, out var externalId))
        {
            switch (item)
            {
                case Person:
                case Book:
                    yield return $"{ComicVineApiUrls.BaseWebsiteUrl}/{externalId}";
                    break;
            }
        }
    }
}
