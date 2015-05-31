using System.Collections.Generic;
using ServiceStack;

namespace OneDrive.RestServices
{
    [Route("/OneDrive/SyncTarget", "POST")]
    public class AddSyncTarget : IReturnVoid
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool EnableForEveryone { get; set; }
        public List<string> UserIds { get; set; }
        public string Code { get; set; }
    }
}
