using MediaBrowser.Plugins.GoogleDrive.Configuration;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public class GoogleCredentials
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public AccessToken AccessToken { get; set; }
    }
}
