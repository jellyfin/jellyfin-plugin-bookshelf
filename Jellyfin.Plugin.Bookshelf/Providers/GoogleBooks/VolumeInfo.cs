using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    /// <summary>
    /// Volume info dto.
    /// </summary>
    public class VolumeInfo
    {
        /// <summary>
        /// Gets or sets the volume title.
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the names of the authors and/or editors for this volume.
        /// </summary>
        [JsonPropertyName("authors")]
        public IReadOnlyList<string> Authors { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the date of publication.
        /// </summary>
        [JsonPropertyName("publishedDate")]
        public string? PublishedDate { get; set; }

        /// <summary>
        /// Gets or sets a list of image links for all the sizes that are available.
        /// </summary>
        [JsonPropertyName("imageLinks")]
        public ImageLinks? ImageLinks { get; set; }

        /// <summary>
        /// Gets or sets the publisher of this volume.
        /// </summary>
        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }

        /// <summary>
        /// Gets or sets the synopsis of the volume.
        /// The text of the description is formatted in HTML and includes simple formatting elements, such as b, i, and br tags.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the main category to which this volume belongs.
        /// It will be the category from the categories list that has the highest weight.
        /// </summary>
        [JsonPropertyName("mainCategory")]
        public string? MainCategory { get; set; }

        /// <summary>
        /// Gets or sets the list of subject categories, such as "Fiction", "Suspense", etc.
        /// </summary>
        [JsonPropertyName("categories")]
        public IReadOnlyList<string> Categories { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the mean review rating for this volume. (min = 1.0, max = 5.0).
        /// </summary>
        [JsonPropertyName("averageRating")]
        public float? AverageRating { get; set; }

        /// <summary>
        /// Gets or sets the best language for this volume (based on content).
        /// It is the two-letter ISO 639-1 code such as 'fr', 'en', etc.
        /// </summary>
        [JsonPropertyName("language")]
        public string? Language { get; set; }
    }
}
