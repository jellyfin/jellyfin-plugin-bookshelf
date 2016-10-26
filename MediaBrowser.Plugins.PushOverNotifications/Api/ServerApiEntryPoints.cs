using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Services;
using MediaBrowser.Plugins.PushOverNotifications.Configuration;

namespace MediaBrowser.Plugins.PushOverNotifications.Api
{
    [Route("/Notification/Pushover/Test/{UserID}", "POST", Summary = "Tests Pushover")]
    public class TestNotification : IReturnVoid
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserID { get; set; }
    }

    class ServerApiEndpoints : IService
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public ServerApiEndpoints(ILogManager logManager, IHttpClient httpClient)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _httpClient = httpClient;
        }
        private PushOverOptions GetOptions(String userID)
        {
            return Plugin.Instance.Configuration.Options
                .FirstOrDefault(i => string.Equals(i.MediaBrowserUserId, userID, StringComparison.OrdinalIgnoreCase));
        }

        public object Post(TestNotification request)
        {
            var options = GetOptions(request.UserID);

            var parameters = new Dictionary<string, string>
            {
                {"token", options.Token},
                {"user", options.UserKey},
                {"title", "Test Notification" },
                {"message", "This is a test notification from MediaBrowser"}
            };

            _logger.Debug("Pushover <TEST> to {0} - {1}", options.Token, options.UserKey);

            return _httpClient.Post(new HttpRequestOptions { Url = "https://api.pushover.net/1/messages.json" }, parameters);
        }
    }
}
