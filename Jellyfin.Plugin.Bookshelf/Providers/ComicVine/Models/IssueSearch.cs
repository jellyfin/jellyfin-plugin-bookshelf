namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    /// <summary>
    /// Result of a search on the Issue resource.
    /// </summary>
    public class IssueSearch
    {
        /// <summary>
        /// Gets the list of aliases the issue is known by. A \n (newline) seperates each alias.
        /// </summary>
        public string Aliases { get; init; } = string.Empty;

        /// <summary>
        /// Gets the URL pointing to the issue detail resource.
        /// </summary>
        public string ApiDetailUrl { get; init; } = string.Empty;

        /// <summary>
        /// Gets the publish date printed on the cover of an issue.
        /// </summary>
        public string CoverDate { get; init; } = string.Empty;

        /// <summary>
        /// Gets the date the issue was added to Comic Vine.
        /// </summary>
        public string DateAdded { get; init; } = string.Empty;

        /// <summary>
        /// Gets the date the issue was last updated on Comic Vine.
        /// </summary>
        public string DateLastUpdated { get; init; } = string.Empty;

        /// <summary>
        /// Gets a brief summary of the issue.
        /// </summary>
        public string? Deck { get; init; }

        /// <summary>
        /// Gets the description of the issue.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets a value indicating whether the issue has a staff review.
        /// </summary>
        public bool HasStaffReview { get; init; }

        /// <summary>
        /// Gets the unique ID of the issue.
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// Gets the main image of the issue.
        /// </summary>
        public ImageList? Image { get; init; }

        /// <summary>
        /// Gets the number assigned to the issue within the volume set.
        /// </summary>
        public string IssueNumber { get; init; } = string.Empty;

        /// <summary>
        /// Gets the name of the issue.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the URL pointing to the issue on the Comic Vine website.
        /// </summary>
        public string SiteDetailUrl { get; init; } = string.Empty;

        /// <summary>
        /// Gets the date the issue was first sold in stores.
        /// </summary>
        public string StoreDate { get; init; } = string.Empty;

        /// <summary>
        /// Gets the volume the issue is a part of.
        /// </summary>
        public VolumeOverview? Volume { get; init; }
    }
}
