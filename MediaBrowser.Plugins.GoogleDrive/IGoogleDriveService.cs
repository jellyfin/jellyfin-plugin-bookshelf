using MediaBrowser.Model.Querying;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public interface IGoogleDriveService
    {
        Task<Tuple<string, string>> UploadFile(Stream stream, string[] pathParts, string folderId, GoogleCredentials googleCredentials, IProgress<double> progress, CancellationToken cancellationToken);
        Task<string> GetOrCreateFolder(string name, string parentId, GoogleCredentials googleCredentials, CancellationToken cancellationToken);
        Task DeleteFile(string fileId, GoogleCredentials googleCredentials, CancellationToken cancellationToken);
        Task<string> CreateDownloadUrl(string fileId, GoogleCredentials googleCredentials, CancellationToken cancellationToken);
        Task<QueryResult<FileSystemMetadata>> GetFiles(string id, string rootFolderId, GoogleCredentials googleCredentials, CancellationToken cancellationToken);
        Task<QueryResult<FileSystemMetadata>> GetFiles(string[] pathParts, string rootFolderId, GoogleCredentials googleCredentials, CancellationToken cancellationToken);
        Task<QueryResult<FileSystemMetadata>> GetFiles(string rootFolderId, GoogleCredentials googleCredentials, CancellationToken cancellationToken);
    }
}
