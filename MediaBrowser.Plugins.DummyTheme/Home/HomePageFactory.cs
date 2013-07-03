using MediaBrowser.Theater.Interfaces.Presentation;
using System.Windows.Controls;

namespace MediaBrowser.Plugins.DummyTheme.Home
{
    public class HomePageFactory : IHomePage
    {
        public Page GetPage()
        {
            return new HomePage();
        }

        public string Name
        {
            get { return "Dummy"; }
        }
    }
}
