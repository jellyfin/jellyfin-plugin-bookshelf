using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    /// <summary>
    /// Comic Vine metadata cache manager.
    /// </summary>
    public interface IComicVineMetadataCacheManager
    {
        /// <summary>
        /// Check if the resource is already cached.
        /// </summary>
        /// <param name="apiId">The API id of the resource.</param>
        /// <returns>Whether the resource is cached.</returns>
        public bool HasCache(string apiId);

        /// <summary>
        /// Add an API resource to the cache.
        /// </summary>
        /// <param name="apiId">The API id of the resource.</param>
        /// <param name="resource">The resource to add to the cache.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task AddToCache<T>(string apiId, T resource, CancellationToken cancellationToken);

        /// <summary>
        /// Get an API resource from the cache.
        /// </summary>
        /// <param name="apiId">The API id of the resource.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <returns>The cached resource.</returns>
        public Task<T?> GetFromCache<T>(string apiId, CancellationToken cancellationToken);
    }
}
