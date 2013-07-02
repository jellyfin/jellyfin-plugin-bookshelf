using MediaBrowser.Plugins.Weather.Pages;
using MediaBrowser.Theater.Interfaces.Presentation;
using System.Windows.Controls;

namespace MediaBrowser.Plugins.Weather
{
    public class App : ITheaterApp
    {
        public string Name
        {
            get { return "Weather"; }
        }

        public Page GetLaunchPage()
        {
            return new MainWeatherPage();
        }
    }
}
