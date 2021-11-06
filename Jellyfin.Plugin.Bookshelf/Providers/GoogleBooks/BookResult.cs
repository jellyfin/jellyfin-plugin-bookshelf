using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    /// <summary>
    /// Book result dto.
    /// </summary>
    public class BookResult
    {
        /// <summary>
        /// Gets or sets the book kind.
        /// </summary>
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        /// <summary>
        /// Gets or sets the book id.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the etag.
        /// </summary>
        [JsonPropertyName("etag")]
        public string? Etag { get; set; }

        /// <summary>
        /// Gets or sets the self link.
        /// </summary>
        [JsonPropertyName("selfLink")]
        public string? SelfLink { get; set; }

        /// <summary>
        /// Gets or sets the volume info.
        /// </summary>
        [JsonPropertyName("volumeInfo")]
        public VolumeInfo? VolumeInfo { get; set; }
    }
}
