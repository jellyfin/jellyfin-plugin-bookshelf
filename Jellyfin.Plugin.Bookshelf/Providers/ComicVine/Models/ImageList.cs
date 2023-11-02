namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    /// <summary>
    /// List of images for an issue.
    /// </summary>
    public class ImageList
    {
        /// <summary>
        /// Gets the icon image URL.
        /// </summary>
        public string IconUrl { get; init; } = string.Empty;

        /// <summary>
        /// Gets the medium image URL.
        /// </summary>
        public string MediumUrl { get; init; } = string.Empty;

        /// <summary>
        /// Gets the screen image URL.
        /// </summary>
        public string ScreenUrl { get; init; } = string.Empty;

        /// <summary>
        /// Gets the large screen image URL.
        /// </summary>
        public string ScreenLargeUrl { get; init; } = string.Empty;

        /// <summary>
        /// Gets the small image URL.
        /// </summary>
        public string SmallUrl { get; init; } = string.Empty;

        /// <summary>
        /// Gets the super image URL.
        /// </summary>
        public string SuperUrl { get; init; } = string.Empty;

        /// <summary>
        /// Gets the thumb image URL.
        /// </summary>
        public string ThumbUrl { get; init; } = string.Empty;

        /// <summary>
        /// Gets the tiny image URL.
        /// </summary>
        public string TinyUrl { get; init; } = string.Empty;

        /// <summary>
        /// Gets the original image URL.
        /// </summary>
        public string OriginalUrl { get; init; } = string.Empty;
    }
}
