using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicBookInfo
{
    public class ComicBookInfoFormat
    {
        [JsonPropertyName("appID")]
        public string AppId { get; set; }

        [JsonPropertyName("lastModified")]
        public string LastModified { get; set; }

        [JsonPropertyName("ComicBookInfo/1.0")]
        public ComicBookInfoMetadata Metadata { get; set; }
    }
}
