using Dropbox.Configuration;
using ServiceStack;

namespace Dropbox.RestServices
{
    [Route("/Delete/SyncTarget/{Id}", "GET")]
    public class GetSyncTarget : IReturn<DropboxSyncAccount>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }
}
