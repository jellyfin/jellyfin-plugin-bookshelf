using Jellyfin.Plugin.Bookshelf.Providers;
using Jellyfin.Plugin.Bookshelf.Providers.Audiobook;
using Jellyfin.Plugin.Bookshelf.Providers.ComicBookInfo;
using Jellyfin.Plugin.Bookshelf.Providers.ComicInfo;
using Jellyfin.Plugin.Bookshelf.Providers.ComicVine;
using Jellyfin.Plugin.Bookshelf.Providers.GoogleBooks;
using Jellyfin.Plugin.Bookshelf.Providers.OpenLibrary;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
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
            System.Diagnostics.Debug.WriteLine("BOOKSHELF PLUGIN REGISTERING SERVICES");

            // register the proxy local metadata provider for comic files
            serviceCollection.AddSingleton<ComicFileProvider>();

            // register the actual implementations of the local metadata provider for comic files
            serviceCollection.AddSingleton<IComicFileProvider, ExternalComicInfoProvider>();
            serviceCollection.AddSingleton<IComicFileProvider, InternalComicInfoProvider>();
            serviceCollection.AddSingleton<IComicFileProvider, ComicBookInfoProvider>();

            serviceCollection.AddSingleton<IComicVineMetadataCacheManager, ComicVineMetadataCacheManager>();
            serviceCollection.AddSingleton<IComicVineApiKeyProvider, ComicVineApiKeyProvider>();

            serviceCollection.AddSingleton<GoogleBooksProvider>();
            serviceCollection.AddSingleton<OpenLibraryProvider>();
            serviceCollection.AddSingleton<IRemoteMetadataProvider<Person, PersonLookupInfo>, OpenLibraryPersonProvider>();

            serviceCollection.AddSingleton<ILocalMetadataProvider<AudioBook>, AudiobookMetadataProvider>();
            serviceCollection.AddSingleton<AudiobookMetadataImageProvider>();
        }
    }
}
