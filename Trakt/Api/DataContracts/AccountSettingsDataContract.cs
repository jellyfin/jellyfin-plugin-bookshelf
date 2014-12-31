using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts
{
    [DataContract]
    public class AccountSettingsDataContract
    {
        [DataMember(Name = "status")]
        public string Status { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "profile")]
        public TraktProfileDataContract Profile { get; set; }

        [DataMember(Name = "account")]
        public TraktAccountDataContract Account { get; set; }

        [DataMember(Name = "viewing")]
        public TraktViewingDataContract Viewing { get; set; }

        [DataMember(Name = "connections")]
        public TraktConnectionsDataContract Connections { get; set; }

        [DataMember(Name = "sharing_text")]
        public TraktSharingTextDataContract SharingText { get; set; }

    }



    [DataContract]
    public class TraktProfileDataContract
    {

        [DataMember(Name = "username")]
        public string Username { get; set; }

        [DataMember(Name = "full_name")]
        public string FullName { get; set; }

        [DataMember(Name = "gender")]
        public string Gender { get; set; }

        [DataMember(Name = "age")]
        public string Age { get; set; }

        [DataMember(Name = "location")]
        public string Location { get; set; }

        [DataMember(Name = "about")]
        public string About { get; set; }

        [DataMember(Name = "joined")]
        public int Joined { get; set; }

        [DataMember(Name = "last_login")]
        public int LastLogin { get; set; }

        [DataMember(Name = "avatar")]
        public string Avatar { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

    }



    [DataContract]
    public class TraktAccountDataContract
    {

        [DataMember(Name = "timezone")]
        public string TimeZone { get; set; }

        [DataMember(Name = "use_24hr")]
        public bool Use24Hr { get; set; }

        [DataMember(Name = "protected")]
        public bool Protected { get; set; }

    }



    [DataContract]
    public class TraktViewingDataContract
    {

        [DataMember(Name = "ratings")]
        public TraktViewingRatingsDataContract Ratings { get; set; }

        [DataMember(Name = "shouts")]
        public TraktViewingShoutsDataContract Shouts { get; set; }

    }



    [DataContract]
    public class TraktViewingRatingsDataContract
    {

        [DataMember(Name = "mode")]
        public string Mode { get; set; }

    }



    [DataContract]
    public class TraktViewingShoutsDataContract
    {

        [DataMember(Name = "show_badges")]
        public bool ShowBadges { get; set; }

        [DataMember(Name = "show_spoilers")]
        public bool ShowSpoilers { get; set; }

    }



    [DataContract]
    public class TraktConnectionsDataContract
    {

        [DataMember(Name = "facebook")]
        public TraktConnectionsFacebookDataContract Facebook { get; set; }

        [DataMember(Name = "twitter")]
        public TraktConnectionsTwitterDataContract Twitter { get; set; }

        [DataMember(Name = "tumblr")]
        public TraktConnectionsTumblrDataContract Tumblr { get; set; }

    }



    [DataContract]
    public class TraktConnectionsFacebookDataContract
    {

        [DataMember(Name = "connected")]
        public bool Connected { get; set; }

        [DataMember(Name = "timeline_enabled")]
        public bool TimelineEnabled { get; set; }

        [DataMember(Name = "share_scrobbles_start")]
        public bool ShareScrobblesStart { get; set; }

        [DataMember(Name = "share_scrobbles_end")]
        public bool ShareScrobblesEnd { get; set; }

        [DataMember(Name = "share_tv")]
        public bool ShareTv { get; set; }

        [DataMember(Name = "share_movies")]
        public bool ShareMovies { get; set; }

        [DataMember(Name = "share_ratings")]
        public bool ShareRatings { get; set; }

        [DataMember(Name = "share_checkins")]
        public bool ShareCheckins { get; set; }

    }



    [DataContract]
    public class TraktConnectionsTwitterDataContract
    {

        [DataMember(Name = "connected")]
        public bool Connected { get; set; }

        [DataMember(Name = "share_scrobbles_start")]
        public bool ShareScrobblesStart { get; set; }

        [DataMember(Name = "share_scrobbles_end")]
        public bool ShareScrobblesEnd { get; set; }

        [DataMember(Name = "share_tv")]
        public bool ShareTv { get; set; }

        [DataMember(Name = "share_movies")]
        public bool ShareMovies { get; set; }

        [DataMember(Name = "share_ratings")]
        public bool ShareRatings { get; set; }

        [DataMember(Name = "share_checkins")]
        public bool ShareCheckins { get; set; }

    }



    [DataContract]
    public class TraktConnectionsTumblrDataContract
    {

        [DataMember(Name = "connected")]
        public bool Connected { get; set; }

        [DataMember(Name = "share_scrobbles_start")]
        public bool ShareScrobblesStart { get; set; }

        [DataMember(Name = "share_scrobbles_end")]
        public bool ShareScrobblesEnd { get; set; }

        [DataMember(Name = "share_tv")]
        public bool ShareTv { get; set; }

        [DataMember(Name = "share_movies")]
        public bool ShareMovies { get; set; }

        [DataMember(Name = "share_ratings")]
        public bool ShareRatings { get; set; }

        [DataMember(Name = "share_checkins")]
        public bool ShareCheckins { get; set; }

    }



    [DataContract]
    public class TraktSharingTextDataContract
    {

        [DataMember(Name = "watching")]
        public string Watching { get; set; }

        [DataMember(Name = "watched")]
        public string Watched { get; set; }

    }
}
