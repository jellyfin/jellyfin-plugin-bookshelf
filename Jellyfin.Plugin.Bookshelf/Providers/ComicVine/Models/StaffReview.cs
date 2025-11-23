namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine.Models
{
    /// <summary>
    /// Staff review details.
    /// </summary>
    public class StaffReview
    {
        /// <summary>
        /// Gets the URL pointing to the review detail resource.
        /// </summary>
        public string ApiDetailUrl { get; init; } = string.Empty;

        /// <summary>
        /// Gets the unique ID of the review.
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// Gets the name of the review.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the URL pointing to the review on the Comic Vine website.
        /// </summary>
        public string SiteDetailUrl { get; init; } = string.Empty;
    }
}
