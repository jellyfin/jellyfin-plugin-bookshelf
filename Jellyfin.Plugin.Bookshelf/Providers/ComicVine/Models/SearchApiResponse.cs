using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine.Models;

/// <summary>
/// Comic Vine API response for a search on a resource.
/// </summary>
/// <typeparam name="T">Type of object returned by the response.</typeparam>
public sealed class SearchApiResponse<T> : BaseApiResponse<T>
{
    /// <summary>
    /// Gets zero or more items that match the filters specified.
    /// </summary>
    public IEnumerable<T> Results { get; init; } = Enumerable.Empty<T>();
}
