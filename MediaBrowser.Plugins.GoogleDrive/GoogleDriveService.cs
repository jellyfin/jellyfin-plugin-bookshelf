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
using Interfaces.IO;
using MediaBrowser.Model.Querying;
using File = Google.Apis.Drive.v2.Data.File;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private const string SyncFolderPropertyKey = "CloudSyncFolder";
        private const string SyncFolderPropertyValue = "ba460da6-2cdf-43d8-98fc-ecda617ff1db";

        public async Task<QueryResult<FileMetadata>> GetFiles(FileQuery query, string rootFolderId, GoogleCredentials googleCredentials,
            CancellationToken cancellationToken)
        {
            var fullDriveService = CreateDriveServiceAndCredentials(googleCredentials);
            var driveService = fullDriveService.Item1;

            var result = new QueryResult<FileMetadata>();

            if (!string.IsNullOrWhiteSpace(query.Id))
            {
                try
                {
                    var file = await GetFile(query.Id, driveService, cancellationToken).ConfigureAwait(false);

                    result.TotalRecordCount = 1;
                    result.Items = new[] { file }.Select(GetFileMetadata).ToArray();
                }
                catch (FileNotFoundException)
                {
                    
                }

                return result;
            }

            if (query.FullPath != null && query.FullPath.Length > 0)
            {
                var name = query.FullPath.Last();
                var pathParts = query.FullPath.Take(query.FullPath.Length - 1).ToArray();

                try
                {
                    var parentId = await FindOrCreateParent(driveService, false, pathParts, rootFolderId, cancellationToken)
                                .ConfigureAwait(false);

                    var file = await FindFileId(name, parentId, driveService, cancellationToken).ConfigureAwait(false);

                    result.TotalRecordCount = 1;
                    result.Items = new[] { file }.Select(GetFileMetadata).ToArray();
                }
                catch (FileNotFoundException)
                {

                }

                return result;
            }

            var queryResult = await GetFiles(null, driveService, cancellationToken).ConfigureAwait(false);
            var files = queryResult
                .Select(GetFileMetadata)
                .ToArray();

            result.Items = files;
            result.TotalRecordCount = files.Length;

            return result;
        }

        private FileMetadata GetFileMetadata(File file)
        {
            return new FileMetadata
            {
                IsFolder = string.Equals(file.MimeType, "application/vnd.google-apps.folder", StringComparison.OrdinalIgnoreCase),
                Name = file.Title,
                Id = file.Id,
                MimeType = file.MimeType
            };
        }

        private async Task<string> FindOrCreateParent(DriveService driveService, bool enableCreate, string[] pathParts, string rootParentId, CancellationToken cancellationToken)
        {
            string currentparentId = rootParentId;

            foreach (var part in pathParts)
            {
                currentparentId = await GetOrCreateFolder(part, currentparentId, driveService, cancellationToken);
            }

            return currentparentId;
        }

        public async Task<Tuple<string,string>> UploadFile(Stream stream, string[] pathParts, string folderId, GoogleCredentials googleCredentials, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var name = pathParts.Last();
            pathParts = pathParts.Take(pathParts.Length - 1).ToArray();

            var fullDriveService = CreateDriveServiceAndCredentials(googleCredentials);
            var driveService = fullDriveService.Item1;

            var parentId = await FindOrCreateParent(driveService, true, pathParts, folderId, cancellationToken);
            await TryDeleteFile(parentId, name, driveService, cancellationToken);

            var googleDriveFile = CreateGoogleDriveFile(pathParts, name, folderId);
            googleDriveFile.GoogleDriveFolderId = parentId;

            var file = CreateFileToUpload(googleDriveFile);
            await ExecuteUpload(driveService, stream, file, progress, cancellationToken);

            var uploadedFile = await FindFileId(name, parentId, driveService, cancellationToken);
            return new Tuple<string, string>(uploadedFile.Id, uploadedFile.DownloadUrl + "&access_token=" + fullDriveService.Item2.Token.AccessToken);
        }

        private GoogleDriveFile CreateGoogleDriveFile(string[] pathParts, string name, string folderId)
        {
            var folder = Path.Combine(pathParts);

            return new GoogleDriveFile
            {
                Name = name,
                FolderPath = folder,
                GoogleDriveFolderId = folderId
            };
        }

        public async Task<string> CreateDownloadUrl(string fileId, GoogleCredentials googleCredentials, CancellationToken cancellationToken)
        {
            var fullDriveService = CreateDriveServiceAndCredentials(googleCredentials);
            var driveService = fullDriveService.Item1;

            var uploadedFile = await GetFile(fileId, driveService, cancellationToken);
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

        public async Task<string> GetOrCreateFolder(string name, string parentId, DriveService driveService, CancellationToken cancellationToken)
        {
            var folder = await FindFolder(name, parentId, driveService, cancellationToken);

            if (folder != null)
            {
                return folder.Id;
            }

            return await CreateFolder(name, parentId, cancellationToken, driveService);
        }

        private async Task TryDeleteFile(string parentFolderId, string name, DriveService driveService, CancellationToken cancellationToken)
        {
            try
            {
                var file = await FindFileId(name, parentFolderId, driveService, cancellationToken);

                var request = driveService.Files.Delete(file.Id);
                await request.ExecuteAsync(cancellationToken);
            }
            catch (FileNotFoundException) { }
        }

        public async Task DeleteFile(string fileId, GoogleCredentials googleCredentials, CancellationToken cancellationToken)
        {
            var fullDriveService = CreateDriveServiceAndCredentials(googleCredentials);
            var driveService = fullDriveService.Item1;

            var file = await GetFile(fileId, driveService, cancellationToken);

            var request = driveService.Files.Delete(file.Id);
            await request.ExecuteAsync(cancellationToken);
        }

        public async Task<File> GetFile(string fileId, DriveService driveService, CancellationToken cancellationToken)
        {
            var request = driveService.Files.Get(fileId);
            return await request.ExecuteAsync(cancellationToken);
        }

        public async Task<File> FindFileId(string name, string parentFolderId, DriveService driveService, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }
            if (string.IsNullOrWhiteSpace(parentFolderId))
            {
                throw new ArgumentNullException("parentFolderId");
            }
            var queryName = name.Replace("'", "\\'");
            var query = string.Format("title = '{0}'", queryName);

            query += string.Format(" and '{0}' in parents", parentFolderId);

            var matchingFiles = await GetFiles(query, driveService, cancellationToken);

            var file = matchingFiles.FirstOrDefault();

            if (file == null)
            {
                var message = string.Format("Couldn't find file {0}/{1}", parentFolderId, name);
                throw new FileNotFoundException(message, name);
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
