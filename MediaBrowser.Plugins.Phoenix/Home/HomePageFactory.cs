using MediaBrowser.Model.Dto;
using MediaBrowser.Theater.Interfaces.Presentation;
using System.Windows.Controls;

namespace MediaBrowser.Plugins.Phoenix.Home
{
    public class HomePageFactory : IHomePage
    {
        public string Name
        {
            get { return "Phoenix"; }
        }

        public Page GetHomePage(BaseItemDto rootFolder)
        {
            return new HomePage();
        }
    }
}
