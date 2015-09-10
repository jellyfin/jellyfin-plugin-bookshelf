using System;

namespace OneDrive.Configuration
{
    public class Token
    {
        public DateTime ExpiresAt { get; set; }
        public string AccessToken { get; set; }
        public string RefresToken { get; set; }
    }
}
