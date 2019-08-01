using System.Collections.Generic;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    public class SearchResult
    {
        public string kind { get; set; }
        public int totalItems { get; set; }
        public List<BookResult> items { get; set; }
    }

    public class BookResult
    {
        public string Kind { get; set; }
        public string id { get; set; }
        public string etag { get; set; }
        public string selfLink { get; set; }
        public VolumeInfo volumeInfo { get; set; }
    }

    public class VolumeInfo
    {
        public string title { get; set; }
        public List<string> authors { get; set; }
        public string publishedDate { get; set; }
        public ImageLinks imageLinks { get; set; }
        public string publisher { get; set; }
        public string description { get; set; }
        public string mainCatagory { get; set; }
        public List<string> catagories { get; set; }
        public float averageRating { get; set; }
    }

    public class ImageLinks
    {
        // Only the 2 thumbnail images are available during the initial search
        public string smallThumbnail { get; set; }
        public string thumbnail { get; set; }
        public string small { get; set; }
        public string medium { get; set; }
        public string large { get; set; }
        public string extraLarge { get; set; }
    }
}
