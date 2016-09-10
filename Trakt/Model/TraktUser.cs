using System;

namespace Trakt.Model
{
    public class TraktUser
    {
        public String UserName { get; set; }

        public String Password { get; set; }

        public String LinkedMbUserId { get; set; }

        public bool UsesAdvancedRating { get; set; }

        public String UserToken { get; set; }

        public bool  SkipUnwatchedImportFromTrakt { get; set; }

        public bool PostWatchedHistory { get; set; }

        public bool ExtraLogging { get; set; }

        public bool ExportMediaInfo { get; set; }

        public String[] LocationsExcluded { get; set; }
    }
}
