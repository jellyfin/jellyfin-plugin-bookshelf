using MediaBrowser.Theater.Interfaces.Presentation;

namespace MediaBrowser.Plugins.Weather
{
    public class App : ITheaterApp
    {
        public void Launch()
        {
        }

        public string Name
        {
            get { return "Weather"; }
        }
    }
}
