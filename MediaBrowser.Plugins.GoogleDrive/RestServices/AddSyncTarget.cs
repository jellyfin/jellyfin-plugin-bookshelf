using System.Collections.Generic;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Plugins.GoogleDrive.RestServices
{
    [Route("/GoogleDrive/SyncTarget", "POST")]
    public class AddSyncTarget : IReturnVoid
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool EnableForEveryone { get; set; }
        public List<string> UserIds { get; set; }

        public string Code { get; set; }
        public string RedirectUri { get; set; }
    }
}
