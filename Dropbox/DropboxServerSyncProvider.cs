using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Configuration;
using Interfaces.IO;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;

namespace Dropbox
{
    public class DropboxServerSyncProvider : IServerSyncProvider, IHasDynamicAccess, IRemoteSyncProvider
    {
        // 100mb
        private const int StreamBufferSize = 100 * 1024 * 1024;

        private readonly IConfigurationRetriever _configurationRetriever;
        private readonly IDropboxApi _dropboxApi;
        private readonly IDropboxContentApi _dropboxContentApi;
        private readonly ILogger _logger;

        public DropboxServerSyncProvider(IConfigurationRetriever configurationRetriever, IDropboxApi dropboxApi, IDropboxContentApi dropboxContentApi, ILogManager logManager)
        {
            _configurationRetriever = configurationRetriever;
            _dropboxApi = dropboxApi;
            _dropboxContentApi = dropboxContentApi;
            _logger = logManager.GetLogger("Dropbox");
        }

        public string Name
        {
            get { return Constants.Name; }
        }

        public IEnumerable<SyncTarget> GetAllSyncTargets()
        {
            return _configurationRetriever.GetSyncAccounts().Select(CreateSyncTarget);
        }

        public IEnumerable<SyncTarget> GetSyncTargets(string userId)
        {
            return _configurationRetriever.GetUserSyncAccounts(userId).Select(CreateSyncTarget);
        }

        public async Task<SyncedFileInfo> SendFile(Stream stream, string[] pathParts, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var path = GetFullPath(pathParts, target);
            _logger.Debug("Sending file {0} to {1}", path, target.Name);

            var syncAccount = _configurationRetriever.GetSyncAccount(target.Id);

            await UploadFile(path, stream, syncAccount.AccessToken, cancellationToken);

            return new SyncedFileInfo
            {
                Id = path,
                Path = path,
                Protocol = MediaProtocol.Http
            };
        }

        public string GetFullPath(IEnumerable<string> path, SyncTarget target)
        {
            return string.Join("/", path);
        }

        public async Task<SyncedFileInfo> GetSyncedFileInfo(string id, SyncTarget target, CancellationToken cancellationToken)
        {
            _logger.Debug("Getting synced file info for {0} from {1}", id, target.Name);

            var syncAccount = _configurationRetriever.GetSyncAccount(target.Id);
            var shareResult = await _dropboxApi.Shares(id, syncAccount.AccessToken, cancellationToken);

            return new SyncedFileInfo
            {
                Path = shareResult.url,
                Protocol = MediaProtocol.Http,
                Id = id
            };
        }

        public async Task DeleteFile(string id, SyncTarget target, CancellationToken cancellationToken)
        {
            var syncAccount = _configurationRetriever.GetSyncAccount(target.Id);
            await _dropboxApi.Delete(id, syncAccount.AccessToken, cancellationToken);
        }

        public Task<Stream> GetFile(string id, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<QueryResult<FileMetadata>> GetFiles(FileQuery query, SyncTarget target, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(query.Id))
            {
                // TODO: get file by id
            }

            if (query.FullPath != null && query.FullPath.Length > 0)
            {
                // TODO: get file by path
                var path = GetFullPath(query.FullPath, target);
                _dropboxApi
            }

            // TODO: get all files
        }

        private SyncTarget CreateSyncTarget(DropboxSyncAccount syncAccount)
        {
            return new SyncTarget
            {
                Id = syncAccount.Id,
                Name = syncAccount.Name
            };
        }

        private async Task UploadFile(string path, Stream stream, string accessToken, CancellationToken cancellationToken)
        {
            string uploadId = null;
            var offset = 0;
            var buffer = new byte[StreamBufferSize];
            var numberOfBytesRead = await stream.ReadAsync(buffer, 0, StreamBufferSize, cancellationToken);

            while (numberOfBytesRead > 0)
            {
                var result = await _dropboxContentApi.ChunkedUpload(uploadId, buffer, offset, accessToken, cancellationToken);
                uploadId = result.upload_id;
                offset = result.offset;
                numberOfBytesRead = await stream.ReadAsync(buffer, offset, StreamBufferSize, cancellationToken);
            }

            await _dropboxContentApi.CommitChunkedUpload(path, uploadId, accessToken, cancellationToken);
        }
    }
}
