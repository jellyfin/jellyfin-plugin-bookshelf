using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    public class ComicVineVolumeExternalId : IExternalId
    {
        public static string KeyName => "ComicVineVolume";

        public string Key => KeyName;

        public string ProviderName => "Comic Vine Volume";

        public ExternalIdMediaType? Type
            => null; // TODO: enum does not yet have the Book type

        // TODO: Is there a url?
        public string UrlFormatString =>

            null;

        public bool Supports(IHasProviderIds item)
        {
            return item is Book;
        }
    }
}