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
    public class CustomGameSystemProvider : ICustomMetadataProvider<GameSystem>
    {
        private readonly Task<ItemUpdateType> _cachedResult = Task.FromResult(ItemUpdateType.None);
        private readonly Task<ItemUpdateType> _cachedResultWithUpdate = Task.FromResult(ItemUpdateType.MetadataDownload);

        private readonly IFileSystem _fileSystem;

        public CustomGameSystemProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public Task<ItemUpdateType> FetchAsync(GameSystem item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(item.GameSystemName))
            {
                item.GameSystemName = ResolverHelper.GetGameSystemFromPath(_fileSystem, item.Path);
                return _cachedResultWithUpdate;
            }
            
            return _cachedResult;
        }

        public string Name
        {
            get { return "Game Browser"; }
        }
    }
}
