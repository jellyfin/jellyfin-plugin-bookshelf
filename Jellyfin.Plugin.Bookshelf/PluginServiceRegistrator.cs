using Jellyfin.Plugin.Bookshelf.Providers;
using Jellyfin.Plugin.Bookshelf.Providers.ComicBookInfo;
using Jellyfin.Plugin.Bookshelf.Providers.ComicInfo;
using Jellyfin.Plugin.Bookshelf.Providers.ComicVine;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Bookshelf
{
    /// <summary>
    /// Register Bookshelf services.
    /// </summary>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            // register the proxy local metadata provider for comic files
            serviceCollection.AddSingleton<ComicFileProvider>();

            // register the actual implementations of the local metadata provider for comic files
            serviceCollection.AddSingleton<IComicFileProvider, ExternalComicInfoProvider>();
            serviceCollection.AddSingleton<IComicFileProvider, InternalComicInfoProvider>();
            serviceCollection.AddSingleton<IComicFileProvider, ComicBookInfoProvider>();

            serviceCollection.AddSingleton<IComicVineMetadataCacheManager, ComicVineMetadataCacheManager>();
            serviceCollection.AddSingleton<IComicVineApiKeyProvider, ComicVineApiKeyProvider>();
        }
    }
}
