using MediaBrowser.Theater.Interfaces.Presentation;
using System;

namespace MediaBrowser.Plugins.Phoenix.Home
{
    public class HomePageFactory : IHomePage
    {
        public string Name
        {
            get { return "Phoenix"; }
        }

        public Type PageType
        {
            get { return typeof(HomePage); }
        }
    }
}
