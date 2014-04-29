using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.ProwlNotifications.Configuration;
using ServiceStack;


namespace MediaBrowser.Plugins.ProwlNotifications.Api
{
    [Route("/Notification/Prowl/Test/{UserID}", "POST", Summary = "Tests Prowl")]
    public class TestNotification : IReturnVoid
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserID { get; set; }
    }

    class ServerApiEndpoints : IRestfulService
    {
        private ProwlOptions GetOptions(String userID)
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
                {"description", "This is a test notification from MediaBrowser"}
            };

            using (var client = new WebClient())
            {
                return client.UploadValuesTaskAsync("https://api.prowlapp.com/publicapi/add", parameters);
            }
        }
    }
}
