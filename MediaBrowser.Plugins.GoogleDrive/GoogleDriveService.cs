using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using File = Google.Apis.Drive.v2.Data.File;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private const string PathPropertyKey = "Path";

        public async Task UploadFile(Stream stream, GoogleDriveFile googleDriveFile, GoogleCredentials googleCredentials, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var driveService = CreateDriveService(googleCredentials);

            var file = new File
            {
                Title = googleDriveFile.Name,
                Parents = new List<ParentReference> { new ParentReference { Id = "appfolder" } },
                Properties = new List<Property> { new Property { Key = PathPropertyKey, Value = googleDriveFile.FolderPath } }
            };

            var request = driveService.Files.Insert(file, stream, "application/octet-stream");

            var streamLength = stream.Length;
            request.ProgressChanged += (uploadProgress) => progress.Report(uploadProgress.BytesSent / streamLength * 100);

            await request.UploadAsync(cancellationToken);
        }

        public async Task DeleteFile(GoogleDriveFile googleDriveFile, GoogleCredentials googleCredentials, CancellationToken cancellationToken)
        {
            var driveService = CreateDriveService(googleCredentials);

            var fileId = await FindFileId(googleDriveFile, driveService, cancellationToken);

            var request = driveService.Files.Delete(fileId);
            await request.ExecuteAsync(cancellationToken);
        }

        public async Task<Stream> GetFile(GoogleDriveFile googleDriveFile, GoogleCredentials googleCredentials, CancellationToken cancellationToken)
        {
            var driveService = CreateDriveService(googleCredentials);

            var fileId = await FindFileId(googleDriveFile, driveService, cancellationToken);

            var request = driveService.Files.Get(fileId);
            var stream = new MemoryStream();
            await request.DownloadAsync(stream, cancellationToken);
            return stream;
        }

        public async Task<IEnumerable<GoogleDriveFile>> GetFilesListing(string path, GoogleCredentials googleCredentials, CancellationToken cancellationToken)
        {
            var driveService = CreateDriveService(googleCredentials);

            var files = await GetFiles("'appfolder' in parents", driveService, cancellationToken);

            return files.Where(f => FileIsInPath(f, path))
                .Select(CreateGoogleDriveFile);
        }

        private async Task<string> FindFileId(GoogleDriveFile googleDriveFile, DriveService driveService, CancellationToken cancellationToken)
        {
            var query = string.Format("'appfolder' in parents and title = '{0}'", googleDriveFile.Name);
            var matchingFiles = await GetFiles(query, driveService, cancellationToken);

            var file = matchingFiles.First(f => FileIsInPath(f, googleDriveFile.FolderPath));
            return file.Id;
        }

        private async Task<List<File>> GetFiles(string query, DriveService driveService, CancellationToken cancellationToken)
        {
            var request = driveService.Files.List();
            request.Q = query;

            var files = await GetAllFiles(request, cancellationToken);
            return files;
        }

        private static async Task<List<File>> GetAllFiles(FilesResource.ListRequest request, CancellationToken cancellationToken)
        {
            var result = new List<File>();

            do
            {
                result.AddRange(await GetFilesPage(request, cancellationToken));
            } while (!string.IsNullOrEmpty(request.PageToken));

            return result;
        }

        private static async Task<IEnumerable<File>> GetFilesPage(FilesResource.ListRequest request, CancellationToken cancellationToken)
        {
            var files = await request.ExecuteAsync(cancellationToken);
            request.PageToken = files.NextPageToken;
            return files.Items;
        }

        private bool FileIsInPath(File file, string path)
        {
            var filePath = GetFilePath(file);
            var filePathParts = filePath.Split(Path.DirectorySeparatorChar);
            var pathParts = path.Split(Path.DirectorySeparatorChar);
            return IsSubPath(filePathParts, pathParts);
        }

        private bool IsSubPath(IEnumerable<string> pathParts, IEnumerable<string> subPathParts)
        {
            var parts = pathParts.Zip(subPathParts, (p1, p2) => new Tuple<string, string>(p1, p2));

            return parts.All(part => string.IsNullOrEmpty(part.Item1) || part.Item1 == part.Item2);
        }

        private DriveService CreateDriveService(GoogleCredentials googleCredentials)
        {
            var authorizationCodeFlowInitializer = new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = googleCredentials.ClientId,
                    ClientSecret = googleCredentials.ClientSecret
                }
            };
            var googleAuthorizationCodeFlow = new GoogleAuthorizationCodeFlow(authorizationCodeFlowInitializer);
            var token = new TokenResponse { RefreshToken = googleCredentials.AccessToken.RefreshToken };
            var credentials = new UserCredential(googleAuthorizationCodeFlow, "user", token);

            var initializer = new BaseClientService.Initializer
            {
                ApplicationName = "Media Browser",
                HttpClientInitializer = credentials
            };

            return new DriveService(initializer);
        }

        private GoogleDriveFile CreateGoogleDriveFile(File file)
        {
            return new GoogleDriveFile
            {
                Name = Path.GetFileName(file.Title),
                FolderPath = GetFilePath(file)
            };
        }

        private static string GetFilePath(File file)
        {
            var pathProperty = file.Properties.First(prop => prop.Key == PathPropertyKey);
            return pathProperty.Value;
        }
    }
}
