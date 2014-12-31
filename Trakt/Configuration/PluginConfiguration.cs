using MediaBrowser.Model.Plugins;
using Trakt.Model;

namespace Trakt.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            TraktUsers = new TraktUser[] {};
        }

        public TraktUser[] TraktUsers { get; set; }
    }
}
