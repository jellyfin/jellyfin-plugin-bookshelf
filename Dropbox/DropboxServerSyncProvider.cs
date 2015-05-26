using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Configuration;
using Interfaces.IO;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
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
            return "/" + string.Join("/", path);
        }

        public async Task<SyncedFileInfo> GetSyncedFileInfo(string id, SyncTarget target, CancellationToken cancellationToken)
        {
            _logger.Debug("Getting synced file info for {0} from {1}", id, target.Name);

            try
            {
                return await TryGetSyncedFileInfo(id, target, cancellationToken);
            }
            catch (HttpException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new FileNotFoundException("File not found", ex);
                }

                throw;
            }
        }

        public async Task DeleteFile(string id, SyncTarget target, CancellationToken cancellationToken)
        {
            try
            {
                var syncAccount = _configurationRetriever.GetSyncAccount(target.Id);
                await _dropboxApi.Delete(id, syncAccount.AccessToken, cancellationToken);
            }
            catch (HttpException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new FileNotFoundException("File not found", ex);
                }

                throw;
            }
        }

        public Task<Stream> GetFile(string id, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            try
            {
                var syncAccount = _configurationRetriever.GetSyncAccount(target.Id);
                return _dropboxContentApi.Files(id, syncAccount.AccessToken, cancellationToken);
            }
            catch (HttpException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new FileNotFoundException("File not found", ex);
                }

                throw;
            }
        }

        public async Task<QueryResult<FileMetadata>> GetFiles(FileQuery query, SyncTarget target, CancellationToken cancellationToken)
        {
            try
            {
                return await TryGetFiles(query, target, cancellationToken);
            }
            catch (HttpException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return new QueryResult<FileMetadata>();
                }

                throw;
            }
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
            var buffer = await FillBuffer(stream, 0, cancellationToken);

            while (buffer.Count > 0)
            {
                var result = await _dropboxContentApi.ChunkedUpload(uploadId, buffer.Array, offset, accessToken, cancellationToken);
                uploadId = result.upload_id;
                offset = result.offset;
                buffer = await FillBuffer(stream, offset, cancellationToken);
            }

            await _dropboxContentApi.CommitChunkedUpload(path, uploadId, accessToken, cancellationToken);
        }

        private static async Task<BufferArray> FillBuffer(Stream stream, int offset, CancellationToken cancellationToken)
        {
            if (offset >= stream.Length)
            {
                return new BufferArray();
            }

            var buffer = new byte[StreamBufferSize];
            var numberOfBytesRead = await stream.ReadAsync(buffer, offset, StreamBufferSize, cancellationToken);
            return new BufferArray(buffer, numberOfBytesRead);
        }

        private async Task<SyncedFileInfo> TryGetSyncedFileInfo(string id, SyncTarget target, CancellationToken cancellationToken)
        {
            var syncAccount = _configurationRetriever.GetSyncAccount(target.Id);

            var shareResult = await _dropboxApi.Media(id, syncAccount.AccessToken, cancellationToken);

            return new SyncedFileInfo
            {
                Path = shareResult.url,
                Protocol = MediaProtocol.Http,
                Id = id
            };
        }

        private async Task<QueryResult<FileMetadata>> TryGetFiles(FileQuery query, SyncTarget target, CancellationToken cancellationToken)
        {
            var syncAccount = _configurationRetriever.GetSyncAccount(target.Id);
            var path = FindPathFromFileQuery(query, target);

            if (string.IsNullOrEmpty(path))
            {
                return await FindFilesMetadata(syncAccount.AccessToken, cancellationToken);
            }

            return await FindFileMetadata(path, syncAccount.AccessToken, cancellationToken);
        }

        private string FindPathFromFileQuery(FileQuery query, SyncTarget target)
        {
            if (!string.IsNullOrEmpty(query.Id))
            {
                return query.Id;
            }

            if (query.FullPath != null && query.FullPath.Length > 0)
            {
                return GetFullPath(query.FullPath, target);
            }

            return string.Empty;
        }

        private async Task<QueryResult<FileMetadata>> FindFileMetadata(string path, string accessToken, CancellationToken cancellationToken)
        {
            var metadata = await _dropboxApi.Metadata(path, accessToken, cancellationToken);

            return new QueryResult<FileMetadata>
            {
                Items = new[] { CreateFileMetadata(metadata) },
                TotalRecordCount = 1
            };
        }

        private async Task<QueryResult<FileMetadata>> FindFilesMetadata(string accessToken, CancellationToken cancellationToken)
        {
            var files = new List<FileMetadata>();
            var deltaResult = new DeltaResult { has_more = true };

            while (deltaResult.has_more)
            {
                deltaResult = await _dropboxApi.Delta(deltaResult.cursor, accessToken, cancellationToken);

                var newFiles = deltaResult.entries
                    .Select(deltaEntry => deltaEntry.Metadata)
                    .Where(metadata => metadata != null)
                    .Select(CreateFileMetadata);

                files.AddRange(newFiles);
            }

            return new QueryResult<FileMetadata>
            {
                Items = files.ToArray(),
                TotalRecordCount = files.Count
            };
        }

        private static FileMetadata CreateFileMetadata(MetadataResult metadata)
        {
            return new FileMetadata
            {
                Id = metadata.path,
                IsFolder = metadata.is_dir,
                MimeType = metadata.mime_type,
                Name = metadata.path.Split('/').Last()
            };
        }
    }
}
