using MediaBrowser.Theater.Interfaces.Presentation;
using System;

namespace MediaBrowser.Plugins.DummyTheme.Home
{
    public class HomePageFactory : IHomePage
    {
        public string Name
        {
            get { return "Dummy"; }
        }

        public Type PageType
        {
            get { return typeof(HomePage); }
        }
    }
}
