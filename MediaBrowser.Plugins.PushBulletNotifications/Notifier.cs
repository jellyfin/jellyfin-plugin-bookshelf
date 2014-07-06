using System.Collections.Generic;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.PushBulletNotifications.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.PushBulletNotifications
{
    public class Notifier : INotificationService
    {
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;

        public Notifier(ILogManager logManager, IHttpClient httpClient)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _httpClient = httpClient;
        }

        public bool IsEnabledForUser(User user)
        {
            var options = GetOptions(user);

            return options != null && IsValid(options) && options.Enabled;
        }

        private PushBulletOptions GetOptions(User user)
        {
            return Plugin.Instance.Configuration.Options
                .FirstOrDefault(i => string.Equals(i.MediaBrowserUserId, user.Id.ToString("N"), StringComparison.OrdinalIgnoreCase));
        }

        public string Name
        {
            get { return Plugin.Instance.Name; }
        }

        public Task SendNotification(UserNotification request, CancellationToken cancellationToken)
        {
            
            var options = GetOptions(request.User);

            var parameters = new Dictionary<string, string>
                {
                    {"device_id", options.DeviceId},
                    {"type", "note"},
                    {"title", request.Name},
                    {"body", request.Description}
                };

            _logger.Debug("PushBullet to Token : {0} - {1} - {2}", options.Token, options.DeviceId, request.Description);
            var _httpRequest = new HttpRequestOptions();
            string _cred = string.Format("{0} {1}", "Basic", options.Token);

            _httpRequest.RequestHeaders["Authorization"] = _cred;
            _httpRequest.Url = "https://api.pushbullet.com/api/pushes";

            return _httpClient.Post(_httpRequest, parameters);
        }

        private bool IsValid(PushBulletOptions options)
        {
            return !string.IsNullOrEmpty(options.Token);
        }
    }
}
