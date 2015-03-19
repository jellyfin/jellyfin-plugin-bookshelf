using MediaBrowser.Plugins.GoogleDrive.Configuration;
using ServiceStack;

namespace MediaBrowser.Plugins.GoogleDrive.RestServices
{
    [Route("/GoogleDrive/SyncTarget/{Id}", "GET")]
    public class GetSyncTarget : IReturn<GoogleDriveSyncAccount>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }
}
