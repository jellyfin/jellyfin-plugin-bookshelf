using MediaBrowser.Model.Querying;
using Interfaces.IO;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public interface IGoogleDriveService
    {
        Task<Tuple<string, string>> UploadFile(Stream stream, string[] pathParts, string folderId, GoogleCredentials googleCredentials, IProgress<double> progress, CancellationToken cancellationToken);
        Task<string> GetOrCreateFolder(string name, string parentId, GoogleCredentials googleCredentials, CancellationToken cancellationToken);
        Task DeleteFile(string fileId, GoogleCredentials googleCredentials, CancellationToken cancellationToken);
        Task<string> CreateDownloadUrl(string fileId, GoogleCredentials googleCredentials, CancellationToken cancellationToken);
        Task<QueryResult<FileMetadata>> GetFiles(FileQuery query, string rootFolderId, GoogleCredentials googleCredentials, CancellationToken cancellationToken);
    }
}
