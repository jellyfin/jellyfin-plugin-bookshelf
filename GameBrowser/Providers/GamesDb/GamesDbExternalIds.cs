using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace GameBrowser.Providers.GamesDb
{
    public class GamesDbExternalId : IExternalId
    {
        public string Key
        {
            get { return KeyName; }
        }

        public string Name
        {
            get { return "GamesDb"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Game || item is GameSystem;
        }

        public string UrlFormatString
        {
            get { return "http://thegamesdb.net/game/{0}"; }
        }

        public static string KeyName
        {
            get
            {
                return MetadataProviders.Gamesdb.ToString();
            }
        }
    }
}
