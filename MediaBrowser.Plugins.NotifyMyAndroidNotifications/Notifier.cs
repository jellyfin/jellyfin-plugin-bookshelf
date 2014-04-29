using System.Collections.Specialized;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.NotifyMyAndroidNotifications.Configuration;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.NotifyMyAndroidNotifications
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

        private NotifyMyAndroidOptions GetOptions(User user)
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
                {"apikey", options.Token},
                {"application", "Media Browser"}
            };

            if (string.IsNullOrEmpty(request.Description))
            {
                parameters.Add("event", request.Name);
            }
            else
            {
                parameters.Add("event", request.Name);
                parameters.Add("description", request.Description);
            }


            _logger.Debug("NotifyMyAndroid to {0} - {1} - {2}", options.Token, request.Name, request.Description);

            using (var client = new WebClient())
            {
                return client.UploadValuesTaskAsync("https://www.notifymyandroid.com/publicapi/notify", parameters);
            }
        }

        private bool IsValid(NotifyMyAndroidOptions options)
        {
            return !string.IsNullOrEmpty(options.Token);
        }
    }
}
