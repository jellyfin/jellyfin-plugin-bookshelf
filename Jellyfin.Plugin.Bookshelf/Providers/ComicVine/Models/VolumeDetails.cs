namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine.Models
{
    /// <summary>
    /// Details of a volume.
    /// </summary>
    /// <remarks>The API's volume resource contains more fields but we don't need them for now.</remarks>
    public class VolumeDetails : VolumeOverview
    {
        /// <summary>
        /// Gets the number of issues included in this volume.
        /// </summary>
        public int CountOfIssues { get; init; }

        /// <summary>
        /// Gets the description of the volume.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets the primary publisher a volume is attached to.
        /// </summary>
        public PublisherOverview? Publisher { get; init; }
    }
}
