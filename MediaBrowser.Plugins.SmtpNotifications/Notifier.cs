using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Logging;
using MediaBrowser.Plugins.SmtpNotifications.Configuration;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.SmtpNotifications
{
    public class Notifier : INotificationService
    {
        private readonly IEncryptionManager _encryption;
        private readonly ILogger _logger;
        public static Notifier Instance { get; private set; }

        public Notifier(ILogger logger, IEncryptionManager encryption)
        {
            _encryption = encryption;
            _logger = logger;

            Instance = this;
        }

        public bool IsEnabledForUser(User user)
        {
            var options = GetOptions(user);

            return options != null && IsValid(options) && options.Enabled;
        }

        private SMTPOptions GetOptions(User user)
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

            var mail = new MailMessage(options.EmailFrom, options.EmailTo)
            {
                Subject = "Emby: " + request.Name,
                Body = request.Description
            };

            var client = new SmtpClient
            {
                Host = options.Server,
                Port = options.Port,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = 20000
            };

            if (options.SSL) client.EnableSsl = true;

            _logger.Info("Emailing {0} with subject {1}", options.EmailTo, mail.Subject);

            if (options.UseCredentials)
            {
                var pw = string.IsNullOrWhiteSpace(options.Password) ? _encryption.DecryptString(options.PwData) : options.Password;
                client.Credentials = new NetworkCredential(options.Username, pw);
            }

            return client.SendMailAsync(mail);
        }

        private bool IsValid(SMTPOptions options)
        {
            return !string.IsNullOrEmpty(options.EmailFrom) &&
                   !string.IsNullOrEmpty(options.EmailTo) &&
                   !string.IsNullOrEmpty(options.Server);
        }
    }
}
