using MediaBrowser.Controller.Entities;
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
            return item is AdultVideo;
        }

        public string UrlFormatString
        {
            get
            {
                // TODO: Is there a url for this?
                return null;
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
