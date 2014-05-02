using System.Collections.Generic;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.PushALotNotifications.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.PushALotNotifications
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

            var parameters = new Dictionary<string, string>
                {
                    {"AuthorizationToken", options.Token},
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

            return _httpClient.Post("https://pushalot.com/api/sendmessage", parameters, cancellationToken);
        }

        private bool IsValid(PushALotOptions options)
        {
            return !string.IsNullOrEmpty(options.Token);
        }
    }
}
