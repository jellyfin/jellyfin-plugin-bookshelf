using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    /// <summary>
    /// Book result dto.
    /// </summary>
    public class BookResult
    {
        /// <summary>
        /// Gets or sets the resource type for the volume.
        /// </summary>
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the volume.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the opaque identifier for a specific version of the volume resource.
        /// </summary>
        [JsonPropertyName("etag")]
        public string? Etag { get; set; }

        /// <summary>
        /// Gets or sets the URL to this resource.
        /// </summary>
        [JsonPropertyName("selfLink")]
        public string? SelfLink { get; set; }

        /// <summary>
        /// Gets or sets the general volume information.
        /// </summary>
        [JsonPropertyName("volumeInfo")]
        public VolumeInfo? VolumeInfo { get; set; }
    }
}
