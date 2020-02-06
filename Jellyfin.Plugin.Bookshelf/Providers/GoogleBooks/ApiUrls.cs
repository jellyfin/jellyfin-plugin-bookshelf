namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    public static class GoogleApiUrls
    {
        private const string ApiKey = "AIzaSyCFqC6-BAZwqvOBnbNYN8fbK-R1swtnDac";
        // GoogleBooks API Endpoints
        public const string SearchUrl = @"https://www.googleapis.com/books/v1/volumes?q={0}&startIndex={1}&maxResults={2}";
        public const string DetailsUrl = @"https://www.googleapis.com/books/v1/volumes/{0}";
    }
}
