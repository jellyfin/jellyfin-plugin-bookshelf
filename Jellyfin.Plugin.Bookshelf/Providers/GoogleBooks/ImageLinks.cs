using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks;

/// <summary>
/// Image links dto.
/// </summary>
public class ImageLinks
{
    /// <summary>
    /// Gets or sets the image link for small thumbnail size (width of ~80 pixels).
    /// </summary>
    /// <remarks>
    /// Only the 2 thumbnail images are available during the initial search.
    /// </remarks>
    [JsonPropertyName("smallThumbnail")]
    public string? SmallThumbnail { get; set; }

    /// <summary>
    /// Gets or sets the image link for thumbnail size (width of ~128 pixels).
    /// </summary>
    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    /// <summary>
    /// Gets or sets the image link for small size (width of ~300 pixels).
    /// </summary>
    [JsonPropertyName("small")]
    public string? Small { get; set; }

    /// <summary>
    /// Gets or sets the image link for medium size (width of ~575 pixels).
    /// </summary>
    [JsonPropertyName("medium")]
    public string? Medium { get; set; }

    /// <summary>
    /// Gets or sets the image link for large size (width of ~800 pixels).
    /// </summary>
    [JsonPropertyName("large")]
    public string? Large { get; set; }

    /// <summary>
    /// Gets or sets the image link for extra large size (width of ~1280 pixels).
    /// </summary>
    [JsonPropertyName("extraLarge")]
    public string? ExtraLarge { get; set; }
}
