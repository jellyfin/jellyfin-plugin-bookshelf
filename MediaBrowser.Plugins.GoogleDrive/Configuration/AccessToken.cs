using System;

namespace MediaBrowser.Plugins.GoogleDrive.Configuration
{
    public class AccessToken
    {
        public string Token { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public string RefreshToken { get; set; }
    }
}
