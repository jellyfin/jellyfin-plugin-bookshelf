namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine;

/// <summary>
/// Overview of a volume.
/// </summary>
public class VolumeOverview
{
    /// <summary>
    /// Gets the URL pointing to the volume detail resource.
    /// </summary>
    public string ApiDetailUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets the unique ID of the volume.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets the name of the volume.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the URL pointing to the volume on the Comic Vine website.
    /// </summary>
    public string SiteDetailUrl { get; init; } = string.Empty;
}
