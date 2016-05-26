using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using MovieOrganizer.Configuration;

namespace MovieOrganizer
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        IApplicationPaths _appPaths;

        public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer)
            : base(appPaths, xmlSerializer)
        {
            Instance = this;
            _appPaths = appPaths;
            HtmlHelper.InstallFiles(_appPaths, PluginConfiguration);
        }

        public override string Name
        {
            get { return StaticName; }
        }

        public static string StaticName
        {
            get { return "Movie Organizer"; }
        }

        public override string Description
        {
            get
            {
                return "Allows organizing movies from the Auto-Organize dialog";
            }
        }

        public override void OnUninstalling()
        {
            HtmlHelper.UninstallFiles(_appPaths, PluginConfiguration);
            base.OnUninstalling();
        }

        public static Plugin Instance { get; private set; }
        
        public PluginConfiguration PluginConfiguration
        {
            get { return Configuration; }
        }
    }
}
