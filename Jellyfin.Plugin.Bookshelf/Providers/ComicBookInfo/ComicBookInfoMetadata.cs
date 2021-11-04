using System.Text.Json.Serialization;

#nullable enable
namespace Jellyfin.Plugin.Bookshelf.Providers.ComicBookInfo
{
    public class ComicBookInfoMetadata
    {
        [JsonPropertyName("series")]
        public string? Series { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }

        [JsonPropertyName("publicationMonth")]
        public int? PublicationMonth { get; set; }

        [JsonPropertyName("publicationYear")]
        public int? PublicationYear { get; set; }

        [JsonPropertyName("issue")]
        public int? Issue { get; set; }

        [JsonPropertyName("numberOfIssues")]
        public int? NumberOfIssues { get; set; }

        [JsonPropertyName("volume")]
        public int? Volume { get; set; }

        [JsonPropertyName("numberOfVolumes")]
        public int? NumberOfVolumes { get; set; }

        [JsonPropertyName("rating")]
        public int? Rating { get; set; }

        [JsonPropertyName("genre")]
        public string? Genre { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("credits")]
        public ComicBookInfoCredit[]? Credits { get; set; }

        [JsonPropertyName("tags")]
        public string[]? Tags { get; set; }

        [JsonPropertyName("comments")]
        public string? Comments { get; set; }
    }
}
