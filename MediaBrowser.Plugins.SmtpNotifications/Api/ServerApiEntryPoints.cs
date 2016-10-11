using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Plugins.SmtpNotifications.Configuration;
using ServiceStack;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using System.Threading;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace MediaBrowser.Plugins.SmtpNotifications.Api
{
    [Route("/Notification/SMTP/Test/{UserID}", "POST", Summary = "Tests SMTP")]
    public class TestNotification : IReturnVoid
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserID { get; set; }
    }

    class ServerApiEndpoints : IRestfulService
    {
        private IUserManager _userManager;

        public ServerApiEndpoints(IUserManager userManager)
        {
            _userManager = userManager;
        }

        public void Post(TestNotification request)
        {
            var task = Notifier.Instance.SendNotification(new UserNotification
            {
                Date = DateTime.UtcNow,
                Description = "This is a test notification from Emby Server",
                Level = Model.Notifications.NotificationLevel.Normal,
                Name = "Emby: Test Notification",
                User = _userManager.GetUserById(request.UserID)

            }, CancellationToken.None);

            Task.WaitAll(task);
        }
    }
}
