using System.Collections.Specialized;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.ProwlNotifications.Configuration;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.ProwlNotifications
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

        private ProwlOptions GetOptions(User user)
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


            _logger.Debug("Prowl to {0} - {1} - {2}", options.Token, request.Name, request.Description);

            using (var client = new WebClient())
            {
                return client.UploadValuesTaskAsync("https://api.prowlapp.com/publicapi/add", parameters);
            }
        }

        private bool IsValid(ProwlOptions options)
        {
            return !string.IsNullOrEmpty(options.Token);
        }
    }
}
