namespace OneDrive.Api
{
    public class AuthorizationParameters
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string refresh_token { get; set; }
        public string grant_type { get; set; }
        public string code { get; set; }
    }
}
