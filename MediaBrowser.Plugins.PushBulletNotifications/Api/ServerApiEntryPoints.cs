using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.PushBulletNotifications.Configuration;
using ServiceStack;

namespace MediaBrowser.Plugins.PushBulletNotifications.Api
{
    [Route("/Notification/PushBullet/Test/{UserID}", "POST", Summary = "Tests PushBullet")]
    public class TestNotification : IReturnVoid
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserID { get; set; }
    }

    class ServerApiEndpoints : IRestfulService
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public ServerApiEndpoints(ILogManager logManager, IHttpClient httpClient)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _httpClient = httpClient;
        }
        private PushBulletOptions GetOptions(String userID)
        {
            return Plugin.Instance.Configuration.Options
                .FirstOrDefault(i => string.Equals(i.MediaBrowserUserId, userID, StringComparison.OrdinalIgnoreCase));
        }

        public object Post(TestNotification request)
        {
            var options = GetOptions(request.UserID);

            var parameters = new Dictionary<string, string>
            {
                {"type", "note"},
                {"title", "Test Notification" },
                {"body", "This is a test notification from MediaBrowser"}
            };

            var _httpRequest = new HttpRequestOptions();
            //Create Basic HTTP Auth Header...
            string _cred = string.Format("{0} {1}", "Basic", options.Token);

            _httpRequest.RequestHeaders["Authorization"] = _cred;
            _httpRequest.Url = "https://api.pushbullet.com/api/pushes";

            _logger.Debug("PushBullet <TEST> to {0} - {1}", options.Token, _cred);

            return _httpClient.Post(_httpRequest, parameters);
        }
    }
}
