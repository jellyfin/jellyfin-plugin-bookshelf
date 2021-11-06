using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    /// <summary>
    /// Comic vine volume external id.
    /// </summary>
    public class ComicVineVolumeExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Key => "ComicVineVolume";

        /// <inheritdoc />
        public string ProviderName => "Comic Vine Volume";

        /// <inheritdoc />
        public ExternalIdMediaType? Type
            => null; // TODO: enum does not yet have the Book type

        /// <inheritdoc />
        public string UrlFormatString => string.Empty;

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
        {
            return item is Book;
        }
    }
}
