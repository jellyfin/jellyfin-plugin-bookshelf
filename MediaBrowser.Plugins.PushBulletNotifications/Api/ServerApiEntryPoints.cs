using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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

            //var _httpRequest = (HttpWebRequest)WebRequest.Create("https://api.pushbullet.com/api/pushes");
            //_httpRequest.Credentials = new NetworkCredential();
            //_httpRequest.Method = "POST";

            //var requestWriter = new StreamWriter(_httpRequest.GetRequestStream());


            //try
            //{
            //    requestWriter.Write(SM_TEMPLATE);
            //}
            //finally
            //{
            //    requestWriter.Close();
            //    requestWriter = null;
            //}


            var _httpRequest = new HttpRequestOptions();
            
            //Create Basic HTTP Auth Header...
           // string _cred = string.Format("{0} {1}", "Basic", options.Token);

            string authInfo = options.Token;
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            
           // authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            _httpRequest.RequestHeaders["Authorization"] = "Basic " + authInfo;

            //_httpRequest.
            _httpRequest.Url = "https://api.pushbullet.com/api/pushes";

            _logger.Debug("PushBullet <TEST> to {0} - {1}", options.Token, authInfo);

            return _httpClient.Post(_httpRequest, parameters);
        }
    }
}
