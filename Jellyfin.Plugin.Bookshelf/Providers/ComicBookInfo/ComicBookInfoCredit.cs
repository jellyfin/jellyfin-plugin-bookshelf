using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicBookInfo;

/// <summary>
/// Comic book info credit dto.
/// </summary>
public class ComicBookInfoCredit
{
    /// <summary>
    /// Gets or sets the person name.
    /// </summary>
    [JsonPropertyName("person")]
    public string? Person { get; set; }

    /// <summary>
    /// Gets or sets the role.
    /// </summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }
}
