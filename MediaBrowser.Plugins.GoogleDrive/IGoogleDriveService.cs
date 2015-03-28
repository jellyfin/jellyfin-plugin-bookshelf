using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using File = Google.Apis.Drive.v2.Data.File;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public interface IGoogleDriveService
    {
        Task<string> UploadFile(Stream stream, GoogleDriveFile googleDriveFile, GoogleCredentials googleCredentials, IProgress<double> progress, CancellationToken cancellationToken);
        Task<string> GetOrCreateFolder(string name, GoogleCredentials googleCredentials, CancellationToken cancellationToken);
        Task DeleteFile(GoogleDriveFile googleDriveFile, GoogleCredentials googleCredentials, CancellationToken cancellationToken);
        Task<Stream> GetFile(GoogleDriveFile googleDriveFile, GoogleCredentials googleCredentials, CancellationToken cancellationToken);
        Task<IEnumerable<GoogleDriveFile>> GetFilesListing(string googleDrivefolder, string parentFolderId, GoogleCredentials googleCredentials, CancellationToken cancellationToken);
        Task<File> FindFileId(GoogleDriveFile googleDriveFile, GoogleCredentials googleCredentials, CancellationToken cancellationToken);
    }
}
