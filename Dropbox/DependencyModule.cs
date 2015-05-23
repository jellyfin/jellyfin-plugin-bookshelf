using Dropbox.Api;
using Dropbox.Configuration;
using MediaBrowser.Common;
using MediaBrowser.Common.Plugins;

namespace Dropbox
{
    public class DependencyModule : IDependencyModule
    {
        public void BindDependencies(IDependencyContainer container)
        {
            Bind<IConfigurationRetriever, ConfigurationRetriever>(container);
            Bind<IDropboxApi, DropboxApi>(container);
            Bind<IDropboxContentApi, DropboxContentApi>(container);
        }

        private void Bind<TInterface, TImplementation>(IDependencyContainer container)
            where TImplementation : TInterface
        {
            container.Register(typeof(TInterface), typeof(TImplementation));
        }
    }
}
