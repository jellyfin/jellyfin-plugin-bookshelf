using System.Collections.Specialized;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Notifications;
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
        public bool IsEnabledForUser(User user)
        {
            var options = GetOptions(user);

            return options != null && IsValid(options);
        }

        private PushOverOptions GetOptions(User user)
        {
            return Plugin.Instance.Configuration.Options
                .FirstOrDefault(i => string.Equals(i.MediaBrowserUserId, user.Id.ToString(), StringComparison.OrdinalIgnoreCase));
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
                    {"message", request.Description}
                };

            using (var client = new WebClient())
            {
                return client.UploadValuesTaskAsync("https://api.pushover.net/1/messages.json", parameters);
            }
        }

        private bool IsValid(PushOverOptions options)
        {
            return //options.Enabled &&
                !string.IsNullOrEmpty(options.UserKey) &&
                !string.IsNullOrEmpty(options.Token);
        }
    }
}
