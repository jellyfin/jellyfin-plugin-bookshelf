using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    /// <summary>
    /// Search result dto.
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// Gets or sets the result kind.
        /// </summary>
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        /// <summary>
        /// Gets or sets the total item count.
        /// </summary>
        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }

        /// <summary>
        /// Gets or sets the list of items.
        /// </summary>
        [JsonPropertyName("items")]
        public BookResult[] Items { get; set; } = Array.Empty<BookResult>();
    }
}
