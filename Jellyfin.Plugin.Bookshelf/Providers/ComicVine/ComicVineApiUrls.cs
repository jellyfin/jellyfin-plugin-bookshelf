namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    /// <summary>
    /// Comic Vine API urls.
    /// </summary>
    internal static class ComicVineApiUrls
    {
        /// <summary>
        /// Gets the base url of the website.
        /// </summary>
        public const string BaseWebsiteUrl = @"https://comicvine.gamespot.com";

        /// <summary>
        /// Gets the base url of the API.
        /// </summary>
        public const string BaseUrl = @$"{BaseWebsiteUrl}/api";

        /// <summary>
        /// Gets the URL used to search for issues.
        /// </summary>
        public const string IssueSearchUrl = BaseUrl + @"/search?api_key={0}&format=json&resources=issue&query={1}";

        /// <summary>
        /// Gets the URL used to fetch a specific issue.
        /// </summary>
        public const string IssueDetailUrl = BaseUrl + @"/issue/{1}?api_key={0}&format=json";

        /// <summary>
        /// Gets the URL used to fetch a specific volume.
        /// </summary>
        public const string VolumeDetailUrl = BaseUrl + @"/volume/{1}?api_key={0}&format=json&field_list=api_detail_url,id,name,site_detail_url,count_of_issues,description,publisher";

        /// <summary>
        /// Gets the URL used to search for persons.
        /// </summary>
        public const string PersonSearchUrl = BaseUrl + @"/search?api_key={0}&format=json&resources=person&query={1}";

        /// <summary>
        /// Gets the URL used to fetch a specific person.
        /// </summary>
        public const string PersonDetailUrl = BaseUrl + @"/person/{1}?api_key={0}&format=json&field_list=api_detail_url,id,name,site_detail_url,aliases,birth,country,death,deck,description,email,gender,hometown,image,website";
    }
}
