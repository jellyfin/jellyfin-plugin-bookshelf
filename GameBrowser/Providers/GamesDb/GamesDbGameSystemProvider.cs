using GameBrowser.Extensions;
using GameBrowser.Resolvers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CommonIO;

namespace GameBrowser.Providers.GamesDb
{
    public class GamesDbGameSystemProvider : IRemoteMetadataProvider<GameSystem, GameSystemInfo>
    {
        internal static GamesDbGameSystemProvider Current;

        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public GamesDbGameSystemProvider(IApplicationPaths appPaths, IFileSystem fileSystem, IHttpClient httpClient, ILogger logger)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _httpClient = httpClient;
            _logger = logger;

            Current = this;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = Plugin.Instance.TgdbSemiphore
            });
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(GameSystemInfo searchInfo, CancellationToken cancellationToken)
        {
            return new List<RemoteSearchResult>();
        }

        public async Task<MetadataResult<GameSystem>> GetMetadata(GameSystemInfo id, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<GameSystem>();

            var gameId = id.GetProviderId(GamesDbExternalId.KeyName) ?? FindPlatformId(id);

            if (!string.IsNullOrEmpty(gameId))
            {
                await EnsureCacheFile(gameId, cancellationToken).ConfigureAwait(false);

                var path = GetCacheFilePath(gameId);

                var doc = new XmlDocument();
                doc.Load(path);

                result.Item = new GameSystem();
                result.HasMetadata = true;

                result.Item.SetProviderId(GamesDbExternalId.KeyName, gameId);
                ProcessConsoleXml(result.Item, doc);
            }

            return result;
        }

        private readonly Task _cachedResult = Task.FromResult(true);
        
        internal Task EnsureCacheFile(string gamesDbId, CancellationToken cancellationToken)
        {
            var path = GetCacheFilePath(gamesDbId);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 7)
                {
                    return _cachedResult;
                }
            }

            return DownloadGameSystemInfo(gamesDbId, cancellationToken);
        }

        internal async Task DownloadGameSystemInfo(string gamesDbId, CancellationToken cancellationToken)
        {
            var url = string.Format(TgdbUrls.GetPlatform, gamesDbId);

            var xmlPath = GetCacheFilePath(gamesDbId);

            using (var stream = await _httpClient.Get(url, Plugin.Instance.TgdbSemiphore, cancellationToken).ConfigureAwait(false))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(xmlPath));

                using (var fileStream = _fileSystem.GetFileStream(xmlPath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }
        }

        internal string GetCacheFilePath(string gamesDbId)
        {
            var gameDataPath = GetGamesDataPath();
            return Path.Combine(gameDataPath, gamesDbId, "tgdb.xml");
        }

        private string GetGamesDataPath()
        {
            var dataPath = Path.Combine(_appPaths.CachePath, "tgdb-gamesystems");

            return dataPath;
        }

        private void ProcessConsoleXml(GameSystem console, XmlDocument xmlDocument)
        {
            var platformName = xmlDocument.SafeGetString("//Platform/Platform");

            if (!string.IsNullOrEmpty(platformName))
            {
                console.Name = platformName;
            }

            console.Overview = xmlDocument.SafeGetString("//Platform/overview");
            if (console.Overview != null)
            {
                console.Overview = console.Overview.Replace("\n\n", "\n");
            }
        }

        public string FindPlatformId(GameSystemInfo console)
        {
            var platformSettings = Plugin.Instance.Configuration.GameSystems.FirstOrDefault(gs => console.Path.Equals(gs.Path));

            if (platformSettings != null)
            {
                var id = ResolverHelper.GetTgdbId(platformSettings.ConsoleType);

                if (id != null)
                {
                    return id.ToString();
                }
            }

            return null;
        }

        public string Name
        {
            get { return "GamesDb"; }
        }
    }
}
