using ServiceStack;

namespace MediaBrowser.Plugins.GoogleDrive.RestServices
{
    [Route("/GoogleDrive/Auth/Token", "POST")]
    public class CodeRequest : IReturnVoid
    {
        public string Code { get; set; }
        public string RedirectUri { get; set; }
        public string UserId { get; set; }
    }
}
