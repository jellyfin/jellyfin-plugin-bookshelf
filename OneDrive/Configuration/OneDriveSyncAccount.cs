using System.Collections.Generic;

namespace OneDrive.Configuration
{
    public class OneDriveSyncAccount
    {
        public OneDriveSyncAccount()
        {
            UserIds = new List<string>();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public Token AccessToken { get; set; }
        public bool EnableForEveryone { get; set; }
        public List<string> UserIds { get; set; }
    }
}
