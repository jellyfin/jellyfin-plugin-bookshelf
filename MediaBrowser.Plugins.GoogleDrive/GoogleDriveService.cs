using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using File = Google.Apis.Drive.v2.Data.File;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private const string SyncFolderPropertyKey = "CloudSyncFolder";
        private const string SyncFolderPropertyValue = "ba460da6-2cdf-43d8-98fc-ecda617ff1db";
        private const string PathPropertyKey = "Path";

        private async Task<string> FindOrCreateParent(GoogleCredentials googleCredentials, bool enableCreate, string path, string rootParentId, CancellationToken cancellationToken)
        {
            var parts = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            string currentparentId = rootParentId;

            foreach (var part in parts)
            {
                currentparentId = await GetOrCreateFolder(part, currentparentId, googleCredentials, cancellationToken);
            }

            return currentparentId;
        }

        public async Task<string> UploadFile(Stream stream, GoogleDriveFile googleDriveFile, GoogleCredentials googleCredentials, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var fullDriveService = CreateDriveServiceAndCredentials(googleCredentials);
            var driveService = fullDriveService.Item1;

            var parentId = await FindOrCreateParent(googleCredentials, true, googleDriveFile.FolderPath, googleDriveFile.GoogleDriveFolderId, cancellationToken);
            googleDriveFile.GoogleDriveFolderId = parentId;

            await TryDeleteFile(googleDriveFile, googleCredentials, cancellationToken);

            var file = CreateFileToUpload(googleDriveFile);
            await ExecuteUpload(driveService, stream, file, progress, cancellationToken);

            var uploadedFile = await FindFileId(googleDriveFile, driveService, cancellationToken);
            return uploadedFile.DownloadUrl + "&access_token=" + fullDriveService.Item2.Token.AccessToken;
        }

        public async Task<string> CreateDownloadUrl(GoogleDriveFile googleDriveFile, GoogleCredentials googleCredentials, CancellationToken cancellationToken)
        {
            var fullDriveService = CreateDriveServiceAndCredentials(googleCredentials);
            var driveService = fullDriveService.Item1;

            var uploadedFile = await FindFileId(googleDriveFile, driveService, cancellationToken);
            return uploadedFile.DownloadUrl + "&access_token=" + fullDriveService.Item2.Token.AccessToken;
        }

        public async Task<string> GetOrCreateFolder(string name, string parentId, GoogleCredentials googleCredentials, CancellationToken cancellationToken)
        {
            var driveService = CreateDriveService(googleCredentials);

            var folder = await FindFolder(name, parentId, driveService, cancellationToken);

            if (folder != null)
            {
                return folder.Id;
            }

            return await CreateFolder(name, parentId, cancellationToken, driveService);
        }

        private async Task TryDeleteFile(GoogleDriveFile googleDriveFile, GoogleCredentials googleCredentials, CancellationToken cancellationToken)
        {
            try
            {
                var fullDriveService = CreateDriveServiceAndCredentials(googleCredentials);
                var driveService = fullDriveService.Item1;

                var file = await FindFileId(googleDriveFile, driveService, cancellationToken);

                var request = driveService.Files.Delete(file.Id);
                await request.ExecuteAsync(cancellationToken);
            }
            catch (FileNotFoundException) { }
        }

        public async Task DeleteFile(GoogleDriveFile googleDriveFile, GoogleCredentials googleCredentials, CancellationToken cancellationToken)
        {
            var fullDriveService = CreateDriveServiceAndCredentials(googleCredentials);
            var driveService = fullDriveService.Item1;

            var parentId = await FindOrCreateParent(googleCredentials, false, googleDriveFile.FolderPath, googleDriveFile.GoogleDriveFolderId, cancellationToken);
            googleDriveFile.GoogleDriveFolderId = parentId;

            var file = await FindFileId(googleDriveFile, driveService, cancellationToken);

            var request = driveService.Files.Delete(file.Id);
            await request.ExecuteAsync(cancellationToken);
        }

        public async Task<Stream> GetFile(GoogleDriveFile googleDriveFile, GoogleCredentials googleCredentials, CancellationToken cancellationToken)
        {
            var fullDriveService = CreateDriveServiceAndCredentials(googleCredentials);
            var driveService = fullDriveService.Item1;

            var file = await FindFileId(googleDriveFile, driveService, cancellationToken);

            var request = driveService.Files.Get(file.Id);
            return await request.ExecuteAsStreamAsync(cancellationToken);
        }

        public async Task<IEnumerable<GoogleDriveFile>> GetFilesListing(string path, string parentFolderId, GoogleCredentials googleCredentials, CancellationToken cancellationToken)
        {
            var driveService = CreateDriveService(googleCredentials);

            var query = string.Format("'{0}' in parents", parentFolderId);
            var files = await GetFiles(query, driveService, cancellationToken);

            return files.Where(f => FileIsInPath(f, path))
                .Select(CreateGoogleDriveFile);
        }

        public Task<File> FindFileId(GoogleDriveFile googleDriveFile, DriveService driveService, CancellationToken cancellationToken)
        {
            return FindFileId(googleDriveFile, false, driveService, cancellationToken);
        }

        public async Task<File> FindFileId(GoogleDriveFile googleDriveFile, bool isFolder, DriveService driveService, CancellationToken cancellationToken)
        {
            var queryName = googleDriveFile.Name.Replace("'", "\\'");
            var query = string.Format("title = '{0}'", queryName);

            if (isFolder)
            {
                query += " and mimeType = 'application/vnd.google-apps.folder'";
            }

            var matchingFiles = await GetFiles(query, driveService, cancellationToken);

            var file = matchingFiles.FirstOrDefault(f => FileIsInPath(f, googleDriveFile.FolderPath));

            if (file == null)
            {
                var message = string.Format("Couldn't find file {0}/{1}", googleDriveFile.FolderPath, googleDriveFile.Name);
                throw new FileNotFoundException(message, googleDriveFile.Name);
            }

            return file;
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

        private bool IsSubPath(string[] pathParts, IEnumerable<string> subPathParts)
        {
            return !subPathParts.Where((t, i) => pathParts.Length <= i || t != pathParts[i]).Any();
        }

        private Tuple<DriveService, UserCredential> CreateDriveServiceAndCredentials(GoogleCredentials googleCredentials)
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
            var token = new TokenResponse { RefreshToken = googleCredentials.RefreshToken };
            var credentials = new UserCredential(googleAuthorizationCodeFlow, "user", token);

            var initializer = new BaseClientService.Initializer
            {
                ApplicationName = "Emby",
                HttpClientInitializer = credentials
            };

            var service = new DriveService(initializer)
            {
                HttpClient = { Timeout = TimeSpan.FromHours(1) }
            };

            return new Tuple<DriveService, UserCredential>(service, credentials);
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
            var token = new TokenResponse { RefreshToken = googleCredentials.RefreshToken };
            var credentials = new UserCredential(googleAuthorizationCodeFlow, "user", token);

            var initializer = new BaseClientService.Initializer
            {
                ApplicationName = "Emby",
                HttpClientInitializer = credentials
            };

            return new DriveService(initializer)
            {
                HttpClient = { Timeout = TimeSpan.FromHours(1) }
            };
        }

        private static File CreateFileToUpload(GoogleDriveFile googleDriveFile)
        {
            return new File
            {
                Title = googleDriveFile.Name,
                Parents = new List<ParentReference> { new ParentReference { Kind = "drive#fileLink", Id = googleDriveFile.GoogleDriveFolderId } },
                Properties = new List<Property> { new Property { Key = PathPropertyKey, Value = googleDriveFile.FolderPath } },
                Permissions = new List<Permission> { new Permission { Role = "reader", Type = "anyone" } }
            };
        }

        private static async Task ExecuteUpload(DriveService driveService, Stream stream, File file, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var request = driveService.Files.Insert(file, stream, "application/octet-stream");

            var streamLength = stream.Length;
            request.ProgressChanged += (uploadProgress) => progress.Report(uploadProgress.BytesSent / streamLength * 100);

            await request.UploadAsync(cancellationToken);
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

        private async Task<File> FindFolder(string name, string parentId, DriveService driveService, CancellationToken cancellationToken)
        {
            name = name.Replace("'", "\\'");
            var query = string.Format(@"title = '{0}' and properties has {{ key='{1}' and value='{2}' and visibility='PRIVATE' }}", name, SyncFolderPropertyKey, SyncFolderPropertyValue);

            if (!string.IsNullOrWhiteSpace(parentId))
            {
                query += string.Format(" and '{0}' in parents", parentId);
            }
            var matchingFolders = await GetFiles(query, driveService, cancellationToken);

            return matchingFolders.FirstOrDefault();
        }

        private static async Task<string> CreateFolder(string name, string parentId, CancellationToken cancellationToken, DriveService driveService)
        {
            var file = CreateFolderToUpload(name, parentId);

            var request = driveService.Files.Insert(file);
            var newFolder = await request.ExecuteAsync(cancellationToken);

            return newFolder.Id;
        }

        private static File CreateFolderToUpload(string name, string parentId)
        {
            var property = new Property
            {
                Key = SyncFolderPropertyKey,
                Value = SyncFolderPropertyValue,
                Visibility = "PRIVATE"
            };

            File file = new File
            {
                Title = name,
                MimeType = "application/vnd.google-apps.folder",
                Properties = new List<Property> { property }
            };

            if (!string.IsNullOrWhiteSpace(parentId))
            {
                file.Parents = new List<ParentReference>
                {
                    new ParentReference
                    {
                       Id = parentId
                    }
                };
            }

            return file;
        }
    }
}
