namespace LastfmScrobbler.Models
{
    using System;

    public class LastfmUser
    {
        public string Username { get; set; }

        //We wont store the password, but instead store the session key since its a lifetime key
        public string SessionKey { get; set; }

        public Guid MediaBrowserUserId { get; set; }

        public LastFmUserOptions Options { get; set; }
    }

    public class LastFmUserOptions
    {
        public bool Scrobble       { get; set; }
        public bool SyncFavourites { get; set; }
    }
}
