using MediaBrowser.Common;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Plugins.GoogleDrive.Configuration;

namespace MediaBrowser.Plugins.GoogleDrive.Dependencies
{
    public class DependencyModule : IDependencyModule
    {
        public void BindDependencies(IDependencyContainer container)
        {
            Bind<IConfigurationRetriever, ConfigurationRetriever>(container);
            Bind<IGoogleAuthService, GoogleAuthService>(container);
            Bind<IGoogleDriveService, GoogleDriveService>(container);
        }

        private void Bind<TInterface, TImplementation>(IDependencyContainer container)
            where TImplementation : TInterface
        {
            container.Register(typeof(TInterface), typeof(TImplementation));
        }
    }
}
