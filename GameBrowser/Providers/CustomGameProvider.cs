using GameBrowser.Resolvers;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace GameBrowser.Providers
{
    public class CustomGameProvider : ICustomMetadataProvider<Game>
    {
        private readonly Task<ItemUpdateType> _cachedResult = Task.FromResult(ItemUpdateType.None);
        private readonly Task<ItemUpdateType> _cachedResultWithUpdate = Task.FromResult(ItemUpdateType.MetadataDownload);

        private readonly IFileSystem _fileSystem;

        public CustomGameProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public Task<ItemUpdateType> FetchAsync(Game item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var result = _cachedResult;

            string platform = null;

            if (string.IsNullOrEmpty(item.DisplayMediaType))
            {
                platform = ResolverHelper.AttemptGetGamePlatformTypeFromPath(_fileSystem, item.Path);

                if (!string.IsNullOrEmpty(platform))
                {
                    item.DisplayMediaType = ResolverHelper.GetDisplayMediaTypeFromPlatform(platform);
                    result = _cachedResultWithUpdate;
                }
            }

            if (string.IsNullOrEmpty(item.GameSystem))
            {
                platform = platform ?? ResolverHelper.AttemptGetGamePlatformTypeFromPath(_fileSystem, item.Path);

                if (!string.IsNullOrEmpty(platform))
                {
                    item.GameSystem = ResolverHelper.GetGameSystemFromPlatform(platform);
                    result = _cachedResultWithUpdate;
                }
            }

            return result;
        }

        public string Name
        {
            get { return "Game Browser"; }
        }
    }
}
