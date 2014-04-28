using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Plugins.PushOverNotifications.Configuration;
using ServiceStack;

namespace MediaBrowser.Plugins.PushOverNotifications.Api
{
    [Route("/Notification/Pushover/Test/{UserID}", "POST", Summary = "Tests Pushalot")]
    public class TestNotification : IReturnVoid
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserID { get; set; }
    }

    class ServerApiEndpoints : IRestfulService
    {
        private PushOverOptions GetOptions(String userID)
        {
            return Plugin.Instance.Configuration.Options
                .FirstOrDefault(i => string.Equals(i.MediaBrowserUserId, userID, StringComparison.OrdinalIgnoreCase));
        }

        public object Post(TestNotification request)
        {
            var options = GetOptions(request.UserID);

            var parameters = new NameValueCollection
            {
                {"token", options.Token},
                {"user", options.UserKey},
                {"title", "Test Notification" },
                {"message", "This is a test notification from MediaBrowser"}
            };

            using (var client = new WebClient())
            {
                return client.UploadValuesTaskAsync("https://api.pushover.net/1/messages.json", parameters);
            }
        }
    }
}
