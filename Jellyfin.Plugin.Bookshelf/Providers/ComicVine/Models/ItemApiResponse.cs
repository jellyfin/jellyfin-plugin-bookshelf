namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine.Models;

/// <summary>
/// Comic Vine API response for a specific resource item.
/// </summary>
/// <typeparam name="T">Type of object returned by the response.</typeparam>
public sealed class ItemApiResponse<T> : BaseApiResponse<T>
{
    /// <summary>
    /// Gets the item returned by the response.
    /// </summary>
    public T? Results { get; init; }
}
