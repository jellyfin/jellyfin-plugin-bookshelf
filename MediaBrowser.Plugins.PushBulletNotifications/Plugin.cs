using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.PushBulletNotifications.Configuration;

namespace MediaBrowser.Plugins.PushBulletNotifications
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name
        {
            get { return "PushBullet Notifications"; }
        }

        public override string Description
        {
            get
            {
                return "Sends notifications via PushBullet Service.";
            }
        }

        public static Plugin Instance { get; private set; }
    }
}
