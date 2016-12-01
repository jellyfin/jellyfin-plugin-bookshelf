using MediaBrowser.Controller.Security;
using System;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using System.Threading;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Plugins.SmtpNotifications.Api
{
    [Route("/Notification/SMTP/Test/{UserID}", "POST", Summary = "Tests SMTP")]
    public class TestNotification : IReturnVoid
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserID { get; set; }
    }

    class ServerApiEndpoints : IService
    {
        private readonly IUserManager _userManager;
        private readonly IEncryptionManager _encryption;
        private readonly ILogger _logger;

        public ServerApiEndpoints(IUserManager userManager, ILogger logger, IEncryptionManager encryption)
        {
            _userManager = userManager;
            _logger = logger;
            _encryption = encryption;
        }

        public void Post(TestNotification request)
        {
            var task = new Notifier(_logger, _encryption).SendNotification(new UserNotification
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
