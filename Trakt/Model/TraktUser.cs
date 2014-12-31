using System;

namespace Trakt.Model
{
    public class TraktUser
    {
        public String UserName { get; set; }

        public String PasswordHash { get; set; }

        public String LinkedMbUserId { get; set; }

        public bool UsesAdvancedRating { get; set; }
    }
}
