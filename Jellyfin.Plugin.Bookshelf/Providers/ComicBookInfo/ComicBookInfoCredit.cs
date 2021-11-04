using System.Text.Json.Serialization;

#nullable enable
namespace Jellyfin.Plugin.Bookshelf.Providers.ComicBookInfo
{
    public class ComicBookInfoCredit
    {
        [JsonPropertyName("person")]
        public string? Person { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("primary")]
        public string? Primary { get; set; }
    }
}
