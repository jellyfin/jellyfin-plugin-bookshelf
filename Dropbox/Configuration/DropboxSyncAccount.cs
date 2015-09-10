using System.Collections.Generic;

namespace Dropbox.Configuration
{
    public class DropboxSyncAccount
    {
        public DropboxSyncAccount()
        {
            UserIds = new List<string>();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string AccessToken { get; set; }
        public bool EnableForEveryone { get; set; }
        public List<string> UserIds { get; set; }
    }
}
