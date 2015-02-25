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
        }

        private void Bind<TInterface, TImplementation>(IDependencyContainer container)
        {
            container.Register(typeof(TInterface), typeof(TImplementation));
        }
    }
}
