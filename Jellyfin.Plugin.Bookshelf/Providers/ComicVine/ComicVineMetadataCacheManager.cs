using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Bookshelf.Providers.ComicVine
{
    /// <summary>
    /// Comic Vine metadata cache manager.
    /// </summary>
    public class ComicVineMetadataCacheManager : IComicVineMetadataCacheManager
    {
        /// <summary>
        /// Cache time in days.
        /// </summary>
        private const int CacheTime = 7;

        private readonly ILogger<ComicVineMetadataCacheManager> _logger;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComicVineMetadataCacheManager"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{ComicVineMetadataCacheManager}"/> interface.</param>
        /// <param name="appPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        public ComicVineMetadataCacheManager(ILogger<ComicVineMetadataCacheManager> logger, IApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _logger = logger;
            _appPaths = appPaths;
            _fileSystem = fileSystem;

            // Ensure the cache directory exists
            Directory.CreateDirectory(GetComicVineCachePath());
        }

        private string GetCacheFilePath(string issueApiId)
        {
            return Path.Combine(GetComicVineCachePath(), issueApiId.ToString(CultureInfo.InvariantCulture) + ".json");
        }

        private string GetComicVineCachePath()
        {
            return Path.Combine(_appPaths.CachePath, "comicvine");
        }

        /// <inheritdoc/>
        public bool HasCache(string issueApiId)
        {
            var path = GetCacheFilePath(issueApiId);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= CacheTime)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task AddToCache(string issueApiId, IssueDetails issue, CancellationToken cancellationToken)
        {
            var filePath = GetCacheFilePath(issueApiId);
            using FileStream fileStream = AsyncFile.OpenWrite(filePath);
            await JsonSerializer.SerializeAsync<IssueDetails>(fileStream, issue, _jsonOptions, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IssueDetails?> GetFromCache(string issueApiId, CancellationToken cancellationToken)
        {
            var filePath = GetCacheFilePath(issueApiId);
            using FileStream fileStream = AsyncFile.OpenRead(filePath);
            var issue = await JsonSerializer.DeserializeAsync<IssueDetails>(fileStream, _jsonOptions, cancellationToken).ConfigureAwait(false);

            return issue;
        }
    }
}
