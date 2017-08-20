using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
using OneDrive.Api;
using OneDrive.Configuration;

namespace OneDrive
{
    public class OneDriveServerSyncProvider : IServerSyncProvider, IHasDynamicAccess, IRemoteSyncProvider
    {
        // 10mb
        private const long StreamBufferSize = 10 * 1024 * 1024;

        private readonly IConfigurationRetriever _configurationRetriever;
        private readonly IOneDriveApi _oneDriveApi;
        private readonly ILiveAuthenticationApi _liveAuthenticationApi;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public OneDriveServerSyncProvider(IConfigurationRetriever configurationRetriever, IOneDriveApi oneDriveApi, ILiveAuthenticationApi liveAuthenticationApi, IHttpClient httpClient, ILogManager logManager)
        {
            _configurationRetriever = configurationRetriever;
            _oneDriveApi = oneDriveApi;
            _liveAuthenticationApi = liveAuthenticationApi;
            _httpClient = httpClient;
            _logger = logManager.GetLogger("OneDrive");
        }

        public string Name
        {
            get { return Constants.Name; }
        }

        public List<SyncTarget> GetAllSyncTargets()
        {
            return _configurationRetriever.GetSyncAccounts().Select(CreateSyncTarget).ToList();
        }

        public List<SyncTarget> GetSyncTargets(string userId)
        {
            return _configurationRetriever.GetUserSyncAccounts(userId).Select(CreateSyncTarget).ToList();
        }

        public async Task<SyncedFileInfo> SendFile(Stream stream, string[] pathParts, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            string path = GetFullPath(pathParts);
            _logger.Debug("Sending file {0} to {1}", path, target.Name);

            var oneDriveCredentials = CreateOneDriveCredentials(target);

            await CreateFolderHierarchy(pathParts, oneDriveCredentials, cancellationToken);

            var uploadSession = await _oneDriveApi.CreateUploadSession(path, oneDriveCredentials, cancellationToken);
            var id = await UploadFile(uploadSession.uploadUrl, stream, oneDriveCredentials, cancellationToken);

            return new SyncedFileInfo
            {
                Id = id,
                Path = path,
                Protocol = MediaProtocol.Http
            };
        }

        public async Task DeleteFile(string id, SyncTarget target, CancellationToken cancellationToken)
        {
            try
            {
                var oneDriveCredentials = CreateOneDriveCredentials(target);

                await _oneDriveApi.Delete(id, oneDriveCredentials, cancellationToken);
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

        public async Task<Stream> GetFile(string id, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var link = await CreateLink(id, target, cancellationToken);

            return await _httpClient.Get(new HttpRequestOptions
            {
                Url = link.Path,
                BufferContent = true,
                CancellationToken = cancellationToken

            });
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

        public async Task<SyncedFileInfo> GetSyncedFileInfo(string id, SyncTarget target, CancellationToken cancellationToken)
        {
            _logger.Debug("Getting synced file info for {0} from {1}", id, target.Name);

            try
            {
                return await CreateLink(id, target, cancellationToken);
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

        private SyncTarget CreateSyncTarget(OneDriveSyncAccount syncAccount)
        {
            return new SyncTarget
            {
                Id = syncAccount.Id,
                Name = syncAccount.Name
            };
        }

        private OneDriveCredentials CreateOneDriveCredentials(SyncTarget target)
        {
            return new OneDriveCredentials(_configurationRetriever, _liveAuthenticationApi, target);
        }

        private string GetFullPath(IEnumerable<string> path)
        {
            var encodedPath = path.Select(EncodePath);
            return string.Join("/", encodedPath);
        }

        private string EncodePath(string str)
        {
            var builder = new UriBuilder("https://api.onedrive.com") { Path = "/" + str };
            return builder.Uri.AbsolutePath.Substring(1);
        }

        private async Task CreateFolderHierarchy(string[] pathParts, OneDriveCredentials credentials, CancellationToken cancellationToken)
        {
            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                try
                {
                    var folder = GetFullPath(pathParts.Take(i));
                    var name = pathParts[i];

                    _logger.Debug("Creating folder {0}/{1}", folder, name);

                    await _oneDriveApi.CreateFolder(folder, name, credentials, cancellationToken);
                }
                catch
                {
                    _logger.Debug("Folder already exists.");
                }
            }
        }

        private async Task<string> UploadFile(string url, Stream stream, OneDriveCredentials oneDriveCredentials, CancellationToken cancellationToken)
        {
            while (true)
            {
                var startIndex = stream.Position;
                var buffer = await FillBuffer(stream, cancellationToken);
                var endIndex = startIndex + buffer.Length - 1;

                var uploadSession = await _oneDriveApi.UploadFragment(url, startIndex, endIndex, stream.Length, buffer, oneDriveCredentials, cancellationToken);

                if (!string.IsNullOrEmpty(uploadSession.id))
                {
                    return uploadSession.id;
                }
            }
        }

        private static async Task<byte[]> FillBuffer(Stream stream, CancellationToken cancellationToken)
        {
            var count = stream.Length - stream.Position;
            var bufferSize = Math.Min(StreamBufferSize, count);

            var buffer = new byte[bufferSize];
            await stream.ReadAsync(buffer, 0, (int)bufferSize, cancellationToken);

            return buffer;
        }

        private async Task<QueryResult<FileMetadata>> TryGetFiles(FileQuery query, SyncTarget target, CancellationToken cancellationToken)
        {
            var oneDriveCredentials = CreateOneDriveCredentials(target);

            if (!string.IsNullOrEmpty(query.Id))
            {
                return await GetFileById(query.Id, oneDriveCredentials, cancellationToken);
            }

            if (query.FullPath != null && query.FullPath.Length > 0)
            {
                var path = GetFullPath(query.FullPath);
                return await GetFileByPath(path, oneDriveCredentials, cancellationToken);
            }

            return await GetAllFiles(oneDriveCredentials, cancellationToken);
        }

        private async Task<QueryResult<FileMetadata>> GetFileById(string id, OneDriveCredentials oneDriveCredentials, CancellationToken cancellationToken)
        {
            var viewChangeResult = await _oneDriveApi.ViewChangeById(id, oneDriveCredentials, cancellationToken);
            var viewChanges = viewChangeResult.value.Select(CreateFileMetadata).ToArray();

            return new QueryResult<FileMetadata>
            {
                Items = viewChanges,
                TotalRecordCount = viewChanges.Length
            };
        }

        private async Task<QueryResult<FileMetadata>> GetFileByPath(string path, OneDriveCredentials oneDriveCredentials, CancellationToken cancellationToken)
        {
            var viewChangeResult = await _oneDriveApi.ViewChangeByPath(path, oneDriveCredentials, cancellationToken);
            var viewChanges = viewChangeResult.value.Select(CreateFileMetadata).ToArray();

            return new QueryResult<FileMetadata>
            {
                Items = viewChanges,
                TotalRecordCount = viewChanges.Length
            };
        }

        private async Task<QueryResult<FileMetadata>> GetAllFiles(OneDriveCredentials oneDriveCredentials, CancellationToken cancellationToken)
        {
            var viewChangeResult = new ViewChangesResult { HasMoreChanges = true };
            var files = new List<FileMetadata>();

            while (viewChangeResult.HasMoreChanges)
            {
                viewChangeResult = await _oneDriveApi.ViewChanges(viewChangeResult.Token, oneDriveCredentials, cancellationToken);
                var newFiles = viewChangeResult.value.Select(CreateFileMetadata);
                files.AddRange(newFiles);
            }

            return new QueryResult<FileMetadata>
            {
                Items = files.ToArray(),
                TotalRecordCount = files.Count
            };
        }

        private FileMetadata CreateFileMetadata(ViewChange viewChange)
        {
            return new FileMetadata
            {
                Id = viewChange.id,
                Name = viewChange.name,
                IsFolder = viewChange.folder != null
            };
        }

        private async Task<SyncedFileInfo> CreateLink(string id, SyncTarget target, CancellationToken cancellationToken)
        {
            var oneDriveCredentials = CreateOneDriveCredentials(target);

            var link = await _oneDriveApi.CreateLink(id, oneDriveCredentials, cancellationToken);

            var url = GetRedirectedUrl(link.link.webUrl.Replace("/redir", "/download"));

            return new SyncedFileInfo
            {
                Id = link.id,
                Path = url,
                Protocol = MediaProtocol.Http
            };
        }

        private string GetRedirectedUrl(string url)
        {
            var request = WebRequest.CreateHttp(url);
            request.Method = "GET";
            request.AllowAutoRedirect = false;

            var response = request.GetResponse();
            return response.Headers["Location"];
        }
    }
}
