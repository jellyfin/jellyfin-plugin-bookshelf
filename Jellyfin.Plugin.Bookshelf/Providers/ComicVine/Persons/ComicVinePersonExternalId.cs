using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    /// <inheritdoc />
    public class ComicVinePersonExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => ComicVineConstants.ProviderName;

        /// <inheritdoc />
        public string Key => ComicVineConstants.ProviderId;

        /// <inheritdoc />
        public ExternalIdMediaType? Type => ExternalIdMediaType.Person;

        /// <inheritdoc />
        public string? UrlFormatString => ComicVineApiUrls.BaseWebsiteUrl + "/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Person;
    }
}
