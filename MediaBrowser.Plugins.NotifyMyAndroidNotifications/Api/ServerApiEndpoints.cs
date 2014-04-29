using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.NotifyMyAndroidNotifications.Configuration;
using ServiceStack;


namespace MediaBrowser.Plugins.NotifyMyAndroidNotifications.Api
{
    [Route("/Notification/NotifyMyAndroid/Test/{UserID}", "POST", Summary = "Tests NotifyMyAndroid")]
    public class TestNotification : IReturnVoid
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserID { get; set; }
    }

    class ServerApiEndpoints : IRestfulService
    {
        private NotifyMyAndroidOptions GetOptions(String userID)
        {
            return Plugin.Instance.Configuration.Options
                .FirstOrDefault(i => string.Equals(i.MediaBrowserUserId, userID, StringComparison.OrdinalIgnoreCase));
        }

        public object Post(TestNotification request)
        {
            var options = GetOptions(request.UserID);

            var parameters = new NameValueCollection
            {
                {"apikey", options.Token},
                {"event", "Test Notification"},
                {"description", "This is a test notification from MediaBrowser"},
                {"application", "Media Browser"}
            };

            using (var client = new WebClient())
            {
                return client.UploadValuesTaskAsync("https://www.notifymyandroid.com/publicapi/notify", parameters);
            }
        }
    }
}
