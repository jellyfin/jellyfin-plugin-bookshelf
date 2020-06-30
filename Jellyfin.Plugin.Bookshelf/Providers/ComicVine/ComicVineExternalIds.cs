using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    public class ComicVineVolumeExternalId : IExternalId
    {
        public string Key
        {
            get { return KeyName; }
        }

        public string ProviderName
        {
            get { return "Comic Vine Volume"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Book;
        }

        public ExternalIdMediaType? Type
            => null; // TODO: enum does not yet have the Book type

        public string UrlFormatString
        {
            get
            {
                // TODO: Is there a url?
                return null;
            }
        }

        public static string KeyName
        {
            get
            {
                return "ComicVineVolume";
            }
        }
    }
}
