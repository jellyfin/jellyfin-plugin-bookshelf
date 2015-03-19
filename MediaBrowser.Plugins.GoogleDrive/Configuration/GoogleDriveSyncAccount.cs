using System.Collections.Generic;

namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public class GoogleDriveSyncAccount
    {
        public GoogleDriveSyncAccount()
        {
            UserIds = new List<string>();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string RefreshToken { get; set; }
        public string FolderId { get; set; }
        public bool EnableForEveryone { get; set; }
        public List<string> UserIds { get; set; }
    }
}
