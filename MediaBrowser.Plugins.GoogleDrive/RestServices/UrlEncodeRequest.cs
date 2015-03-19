using ServiceStack;

namespace MediaBrowser.Plugins.GoogleDrive.RestServices
{
    [Route("/GoogleDrive/SyncTarget/UrlEncode", "GET")]
    public class UrlEncodeRequest : IReturn<string>
    {
        [ApiMember(Name = "Str", Description = "String to UrlEncode", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Str { get; set; }
    }
}
