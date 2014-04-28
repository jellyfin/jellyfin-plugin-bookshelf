using System.Collections.Specialized;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.PushOverNotifications.Configuration;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.PushOverNotifications
{
    public class Notifier : INotificationService
    {
        private readonly ILogger _logger;

        public Notifier(ILogManager logManager)
        {
            _logger = logManager.GetLogger(GetType().Name);
        }

        public bool IsEnabledForUser(User user)
        {
            var options = GetOptions(user);

            return options != null && IsValid(options) && options.Enabled;
        }

        private PushOverOptions GetOptions(User user)
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

            var parameters = new NameValueCollection
                {
                    {"token", options.Token},
                    {"user", options.UserKey},
                };

            if (!string.IsNullOrEmpty(options.DeviceName))
            {
                parameters.Add("device", options.DeviceName);
            }

            if (string.IsNullOrEmpty(request.Description))
            {
                parameters.Add("message", request.Name);
            }
            else
            {
                parameters.Add("title", request.Name);
                parameters.Add("message", request.Description);
            }

            _logger.Debug("PushOver to Token : {0} - {1} - {2}", options.Token, options.UserKey, request.Description);

            using (var client = new WebClient())
            {
                return client.UploadValuesTaskAsync("https://api.pushover.net/1/messages.json", parameters);
            }
        }

        private bool IsValid(PushOverOptions options)
        {
            return !string.IsNullOrEmpty(options.UserKey) &&
                !string.IsNullOrEmpty(options.Token);
        }
    }
}
