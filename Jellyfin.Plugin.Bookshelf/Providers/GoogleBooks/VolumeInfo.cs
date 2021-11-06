using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    /// <summary>
    /// Volume info dto.
    /// </summary>
    public class VolumeInfo
    {
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the list of authors.
        /// </summary>
        [JsonPropertyName("authors")]
        public string[] Authors { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the published date.
        /// </summary>
        [JsonPropertyName("publishedDate")]
        public string? PublishedDate { get; set; }

        /// <summary>
        /// Gets or sets the image links.
        /// </summary>
        [JsonPropertyName("imageLinks")]
        public ImageLinks? ImageLinks { get; set; }

        /// <summary>
        /// Gets or sets the publisher.
        /// </summary>
        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the main category.
        /// </summary>
        [JsonPropertyName("mainCategory")]
        public string? MainCategory { get; set; }

        /// <summary>
        /// Gets or sets the list of categories.
        /// </summary>
        [JsonPropertyName("categories")]
        public string[] Categories { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the average rating.
        /// </summary>
        [JsonPropertyName("averageRating")]
        public float AverageRating { get; set; }
    }
}
