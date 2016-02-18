namespace LastfmScrobbler
{
    using Api;
    using MediaBrowser.Common.Net;
    using MediaBrowser.Controller.Net;
    using MediaBrowser.Model.Serialization;
    using ServiceStack;

    [Route("/Lastfm/Login", "POST")]
    [Api(("Calls Last.fm's getMobileSession to get a Last.fm session key"))]
    public class Login
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RestApi : IRestfulService
    {
        private readonly LastfmApiClient _apiClient;

        public RestApi(IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _apiClient = new LastfmApiClient(httpClient, jsonSerializer);
        }

        public object Post(Login request)
        {
            return _apiClient.RequestSession(request.Username, request.Password);
        }
    }
}
