using OneDrive.Configuration;
using ServiceStack;

namespace OneDrive.RestServices
{
    [Route("/OneDrive/SyncTarget/{Id}", "GET")]
    public class GetSyncTarget : IReturn<OneDriveSyncAccount>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }
}
