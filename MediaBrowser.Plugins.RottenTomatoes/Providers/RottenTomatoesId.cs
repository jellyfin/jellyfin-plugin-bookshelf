using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Plugins.RottenTomatoes.Providers
{
    public class RottenTomatoesExternalId : IExternalId
    {
        public string Name
        {
            get { return "Rotten Tomatoes"; }
        }

        public string Key
        {
            get { return KeyName; }
        }

        public string UrlFormatString
        {
            get { return null; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Movie || item is Trailer || item is MusicVideo;
        }

        public static string KeyName
        {
            get { return MetadataProviders.RottenTomatoes.ToString(); }
        }
    }
}
