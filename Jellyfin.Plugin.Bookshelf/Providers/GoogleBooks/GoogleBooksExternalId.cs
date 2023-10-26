using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks
{
    /// <inheritdoc />
    public class GoogleBooksExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => GoogleBooksConstants.ProviderName;

        /// <inheritdoc />
        public string Key => GoogleBooksConstants.ProviderId;

        /// <inheritdoc />
        public ExternalIdMediaType? Type => null; // TODO: No ExternalIdMediaType value for book

        /// <inheritdoc />
        public string? UrlFormatString => "https://books.google.com/books?id={0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Book;
    }
}
