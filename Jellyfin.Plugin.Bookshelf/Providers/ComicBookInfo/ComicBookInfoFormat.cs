using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicBookInfo;

/// <summary>
/// Comic book info format dto.
/// </summary>
public class ComicBookInfoFormat
{
    /// <summary>
    /// Gets or sets the app id.
    /// </summary>
    [JsonPropertyName("appID")]
    public string? AppId { get; set; }

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    [JsonPropertyName("lastModified")]
    public string? LastModified { get; set; }

    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>
    [JsonPropertyName("ComicBookInfo/1.0")]
    public ComicBookInfoMetadata? Metadata { get; set; }
}
