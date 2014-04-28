using System.Collections.Specialized;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.PushALotNotifications.Configuration;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.PushALotNotifications
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

        private PushALotOptions GetOptions(User user)
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
                    {"AuthorizationToken", options.Token},
                    {"Title", request.Name },
                    {"Body", request.Description}
                };

            if (string.IsNullOrEmpty(request.Description))
            {
                parameters.Add("Body", request.Name);
            }
            else
            {
                parameters.Add("Title", request.Name);
                parameters.Add("Body", request.Description);
            }


            _logger.Debug("PushAlot to {0}", options.Token);

            using (var client = new WebClient())
            {
                return client.UploadValuesTaskAsync("https://pushalot.com/api/sendmessage", parameters);
            }
        }

        private bool IsValid(PushALotOptions options)
        {
            return !string.IsNullOrEmpty(options.Token);
        }
    }
}
