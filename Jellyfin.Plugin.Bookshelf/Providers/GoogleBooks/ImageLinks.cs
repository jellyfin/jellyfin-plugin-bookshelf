using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    /// <summary>
    /// Image links dto.
    /// </summary>
    public class ImageLinks
    {
        /// <summary>
        /// Gets or sets the small thumbnail.
        /// </summary>
        /// <remarks>
        /// // Only the 2 thumbnail images are available during the initial search.
        /// </remarks>
        [JsonPropertyName("smallThumbnail")]
        public string? SmallThumbnail { get; set; }

        /// <summary>
        /// Gets or sets the thumbnail.
        /// </summary>
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        /// <summary>
        /// Gets or sets the small image.
        /// </summary>
        [JsonPropertyName("small")]
        public string? Small { get; set; }

        /// <summary>
        /// Gets or sets the medium image.
        /// </summary>
        [JsonPropertyName("medium")]
        public string? Medium { get; set; }

        /// <summary>
        /// Gets or sets the large image.
        /// </summary>
        [JsonPropertyName("large")]
        public string? Large { get; set; }

        /// <summary>
        /// Gets or sets the extra large image.
        /// </summary>
        [JsonPropertyName("extraLarge")]
        public string? ExtraLarge { get; set; }
    }
}
