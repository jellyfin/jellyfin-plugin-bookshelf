using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using MediaBrowser.Plugins.GoogleDrive.Configuration;
using File = Google.Apis.Drive.v2.Data.File;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public class GoogleDriveService : IGoogleDriveService
    {
        public async Task Upload(string filename, string inputFile, GoogleDriveUser googleDriveUser, CancellationToken cancellationToken)
        {
            var driveService = CreateDriveService(googleDriveUser);
            // TODO: set parent to the special app folder
            var file = new File
            {
                Title = filename,
                Parents = new List<ParentReference> { new ParentReference { Id = "appfolder" } }
            };
            var fileStream = new FileStream(inputFile, FileMode.Open);

            // TODO: set content type
            var insert = driveService.Files.Insert(file, fileStream, "contentType");
            insert.OauthToken = googleDriveUser.AccessToken.Token;
            var upload = await insert.UploadAsync(cancellationToken);
            if (upload.Exception != null)
            {
                throw upload.Exception;
            }
        }

        private DriveService CreateDriveService(GoogleDriveUser googleDriveUser)
        {
            var initializer = CreateInitializer(googleDriveUser);
            return new DriveService(initializer);
        }

        private BaseClientService.Initializer CreateInitializer(GoogleDriveUser googleDriveUser)
        {
            // TODO: find out what to pass as an id
            var initializer = new ServiceAccountCredential.Initializer("id");
            var credentials = new ServiceAccountCredential(initializer);

            return new BaseClientService.Initializer
            {
                ApplicationName = "Media Browser",
                HttpClientInitializer = credentials
            };
        }
    }
}
