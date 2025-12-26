namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine.Models;

/// <summary>
/// Overview of a publisher.
/// </summary>
public class PublisherOverview
{
    /// <summary>
    /// Gets the URL pointing to the publisher detail resource.
    /// </summary>
    public string ApiDetailUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets the unique ID of the publisher.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets the name of the publisher.
    /// </summary>
    public string Name { get; init; } = string.Empty;
}
