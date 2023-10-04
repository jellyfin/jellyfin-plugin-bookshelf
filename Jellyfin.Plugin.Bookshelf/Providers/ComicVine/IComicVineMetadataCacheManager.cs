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
        /// Check if the issue is already cached.
        /// </summary>
        /// <param name="issueApiId">The API Id of the issue.</param>
        /// <returns>Whether the issue is cached.</returns>
        public bool HasCache(string issueApiId);

        /// <summary>
        /// Add the issue to the cache.
        /// </summary>
        /// <param name="issueApiId">The API Id of the issue.</param>
        /// <param name="issue">The issue to add to the cache.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task AddToCache(string issueApiId, IssueDetails issue, CancellationToken cancellationToken);

        /// <summary>
        /// Get the issue from the cache.
        /// </summary>
        /// <param name="issueApiId">The API Id of the issue.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The cached issue.</returns>
        public Task<IssueDetails?> GetFromCache(string issueApiId, CancellationToken cancellationToken);
    }
}
