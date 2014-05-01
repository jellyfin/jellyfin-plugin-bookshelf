using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using ServiceStack;
using MediaBrowser.Plugins.PushALotNotifications.Configuration;

namespace MediaBrowser.Plugins.PushALotNotifications.Api
{
    [Route("/Notification/Pushalot/Test/{UserID}", "POST", Summary = "Tests Pushalot")]
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

        private PushALotOptions GetOptions(String userID)
        {
            return Plugin.Instance.Configuration.Options
                .FirstOrDefault(i => string.Equals(i.MediaBrowserUserId, userID, StringComparison.OrdinalIgnoreCase));
        }

        public object Post(TestNotification request)
        {
            var options = GetOptions(request.UserID);

            var parameters = new Dictionary<string, string>
            {
                {"AuthorizationToken", options.Token},
                {"Title", "Test Notification" },
                {"Body", "This is a test notification from MediaBrowser"}
            };

            _logger.Debug("PushAlot <TEST> to {0}", options.Token);

            return _httpClient.Post(new HttpRequestOptions { Url = "https://pushalot.com/api/sendmessage" }, parameters);
        }
    }
}
