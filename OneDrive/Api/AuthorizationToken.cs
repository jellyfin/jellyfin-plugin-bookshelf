namespace OneDrive.Api
{
    public class AuthorizationToken
    {
        public int expires_in { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
    }
}
