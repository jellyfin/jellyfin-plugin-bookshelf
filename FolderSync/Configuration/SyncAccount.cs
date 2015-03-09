using System.Collections.Generic;

namespace FolderSync.Configuration
{
    public class SyncAccount
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public bool EnableAllUsers { get; set; }
        public List<string> UserIds { get; set; }

        public SyncAccount()
        {
            UserIds = new List<string>();
        }
    }
}
