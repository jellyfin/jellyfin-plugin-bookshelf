using MediaBrowser.Common;
using MediaBrowser.Common.Plugins;
using OneDrive.Api;
using OneDrive.Configuration;

namespace OneDrive.Dependencies
{
    public class DependencyModule : IDependencyModule
    {
        public void BindDependencies(IDependencyContainer container)
        {
            Bind<IConfigurationRetriever, ConfigurationRetriever>(container);
            Bind<ILiveAuthenticationApi, LiveAuthenticationApi>(container);
            Bind<IOneDriveApi, OneDriveApi>(container);
        }

        private void Bind<TInterface, TImplementation>(IDependencyContainer container)
            where TImplementation : TInterface
        {
            container.Register(typeof(TInterface), typeof(TImplementation));
        }
    }
}
