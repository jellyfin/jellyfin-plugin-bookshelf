using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MBBookshelf.Providers.ComicVine
{
    public class ComicVineVolumeExternalId : IExternalId
    {
        public string Key
        {
            get { return KeyName; }
        }

        public string Name
        {
            get { return "Comic Vine Volume"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Book;
        }

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
