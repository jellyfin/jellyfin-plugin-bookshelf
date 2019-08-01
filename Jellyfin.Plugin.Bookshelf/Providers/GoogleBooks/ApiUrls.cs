namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    public static class GoogleApiUrls
    {
        private const string ApiKey = "AIzaSyAs5if3sOZBf54gxCqc4OXqiQl9XPVnBJ8";
        // GoogleBooks API Endpoints
        public const string SearchUrl = @"https://www.googleapis.com/books/v1/volumes?q={0}&startIndex={1}&maxResults={2}&key=" + ApiKey;
        public const string DetailsUrl = @"https://www.googleapis.com/books/v1/volumes/{0}?key=" + ApiKey;
    }
}
