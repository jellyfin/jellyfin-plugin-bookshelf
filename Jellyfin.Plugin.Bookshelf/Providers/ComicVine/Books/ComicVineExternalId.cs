using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    /// <inheritdoc />
    public class ComicVineExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => ComicVineConstants.ProviderName;

        /// <inheritdoc />
        public string Key => ComicVineConstants.ProviderId;

        /// <inheritdoc />
        public ExternalIdMediaType? Type => null; // TODO: No ExternalIdMediaType value for book

        /// <inheritdoc />
        public string? UrlFormatString => ComicVineApiUrls.BaseWebsiteUrl + "/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Book;
    }
}
