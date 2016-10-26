using MediaBrowser.Model.Services;

namespace MediaBrowser.Plugins.GoogleDrive.RestServices
{
    [Route("/GoogleDrive/SyncTarget/{Id}", "DELETE")]
    public class DeleteSyncTarget : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }
}
