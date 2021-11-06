using MediaBrowser.Common.Plugins;
using Jellyfin.Plugin.Bookshelf.Providers.ComicBook;
using Jellyfin.Plugin.Bookshelf.Providers.ComicBookInfo;
using Jellyfin.Plugin.Bookshelf.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Bookshelf
{
    /// <summary>
    /// Register Bookshelf services.
    /// </summary>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            // register the proxy local metadata provider for comic files
            serviceCollection.AddSingleton<ComicFileProvider>();

            // register the actual implementations of the local metadata provider for comic files
            serviceCollection.AddSingleton<IComicFileProvider, ExternalComicInfoProvider>();
            serviceCollection.AddSingleton<IComicFileProvider, InternalComicInfoProvider>();
            serviceCollection.AddSingleton<IComicFileProvider, ComicBookInfoProvider>();
        }
    }
}
