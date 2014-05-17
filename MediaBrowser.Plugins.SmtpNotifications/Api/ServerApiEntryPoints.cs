using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using MediaBrowser.Controller.Net;
using MediaBrowser.Plugins.SmtpNotifications.Configuration;
using ServiceStack;

namespace MediaBrowser.Plugins.SmtpNotifications.Api
{
    [Route("/Notification/SMTP/Test/{UserID}", "POST", Summary = "Tests SMTP")]
    public class TestNotification : IReturnVoid
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserID { get; set; }
    }

    class ServerApiEndpoints : IRestfulService
    {
        private SMTPOptions GetOptions(String userID)
        {
            return Plugin.Instance.Configuration.Options
                .FirstOrDefault(i => string.Equals(i.MediaBrowserUserId, userID, StringComparison.OrdinalIgnoreCase));
        }

        public object Post(TestNotification request)
        {
            var options = GetOptions(request.UserID);

            var mail = new MailMessage(options.EmailFrom, options.EmailTo)
            {
                Subject = "Media Browser: Test Notification",
                Body = "This is a test notification from MediaBrowser"
            };

            var client = new SmtpClient
            {
                Host = options.Server,
                Port = options.Port,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (options.SSL) client.EnableSsl = true;

            if (options.UseCredentials)
            {
                var pw = Encoding.Default.GetString(ProtectedData.Unprotect(Encoding.Default.GetBytes(options.PwData), null, DataProtectionScope.LocalMachine));
                client.Credentials = new NetworkCredential(options.Username, pw);
            }

            return client.SendMailAsync(mail);
        }
    }
}
