namespace LastfmScrobbler.Utils
{
    using MediaBrowser.Controller.Entities;
    using Models;
    using System;
    using System.Linq;

    public static class UserHelpers
    {
        public static LastfmUser GetUser(User user)
        {
            if (user == null)
                return null;

            if (Plugin.Instance.PluginConfiguration.LastfmUsers == null)
                return null;

            return GetUser(user.Id);
        }

        public static LastfmUser GetUser(Guid userId)
        {
            return Plugin.Instance.PluginConfiguration.LastfmUsers.FirstOrDefault(u => u.MediaBrowserUserId.Equals(userId));
        }

        public static LastfmUser GetUser(string userGuid)
        {
            Guid g;
            if (Guid.TryParse(userGuid, out g))
                return GetUser(g);

            return null;
        }
    }
}
