using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Plugins.ADEProvider
{
    public class ExternalId : IExternalId
    {
        public string Key
        {
            get { return KeyName; }
        }

        public string Name
        {
            get { return "Adult Dvd Empire"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Movie || item is Person;
        }

        public string UrlFormatString
        {
            get
            {
                return "http://www.adultdvdempire.com/{0}/";
            }
        }

        public static string KeyName
        {
            get
            {
                return "AdultDvdEmpire";
            }
        }
    }
}
