using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
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
        private PushALotOptions GetOptions(String userID)
        {
            return Plugin.Instance.Configuration.Options
                .FirstOrDefault(i => string.Equals(i.MediaBrowserUserId, userID, StringComparison.OrdinalIgnoreCase));
        }

        public object Post(TestNotification request)
        {
            var options = GetOptions(request.UserID);

            var parameters = new NameValueCollection
            {
                {"AuthorizationToken", options.Token},
                {"Title", "Test Notification" },
                {"Body", "This is a test notification from MediaBrowser"}
            };

            using (var client = new WebClient())
            {
                return client.UploadValuesTaskAsync("https://pushalot.com/api/sendmessage", parameters);
            }
        }
    }
}
