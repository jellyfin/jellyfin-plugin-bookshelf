using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts
{
    [DataContract]
    public class TraktActivityDataContract
    {
        [DataMember(Name = "timestamps")]
        public ActivityTimestamps Timestamps { get; set; }

        [DataMember(Name = "activity")]
        public List<Activity> Activities { get; set; }
    }



    [DataContract]
    public class ActivityTimestamps
    {
        [DataMember(Name = "start")]
        public long Start { get; set; }

        [DataMember(Name = "current")]
        public long Current { get; set; }
    }



    [DataContract]
    public class Activity
    {
        [DataMember(Name = "timestamp")]
        public long Timestamp { get; set; }

        [DataMember(Name = "when")]
        public WhenValue When { get; set; }

        [DataMember(Name = "elapsed")]
        public ElapsedValue Elapsed { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "watched")]
        public int Watched { get; set; }

        [DataMember(Name = "action")]
        public string Action { get; set; }

        [DataMember(Name = "user")]
        public TraktFriendDataContract User { get; set; }

        [DataMember(Name = "rating")]
        public string Rating { get; set; }

        [DataMember(Name = "shout")]
        public TraktActivityShoutDataContract Shout { get; set; }

        [DataMember(Name = "episode")]
        public TraktEpisodeDataContract Episode { get; set; }

        [DataMember(Name = "episodes")]
        public List<TraktEpisodeDataContract> Episodes { get; set; }

        [DataMember(Name = "show")]
        public TraktShowDataContract Show { get; set; }

        [DataMember(Name = "movie")]
        public TraktMovieDataContract Movie { get; set; }

        [DataMember(Name = "list")]
        public TraktUserListDataContract List { get; set; }

        [DataMember(Name = "list_item")]
        public TraktActivityListItemDataContract ListItem { get; set; }

    }



    [DataContract]
    public class WhenValue
    {
        [DataMember(Name = "day")]
        public string Day { get; set; }

        [DataMember(Name = "time")]
        public string Time { get; set; }
    }



    [DataContract]
    public class ElapsedValue
    {
        [DataMember(Name = "full")]
        public string Full { get; set; }

        [DataMember(Name = "short")]
        public string Short { get; set; }
    }



    [DataContract]
    public class TraktActivityShoutDataContract
    {
        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "spoiler")]
        public bool Spoiler { get; set; }
    }



    [DataContract]
    public class TraktActivityListItemDataContract
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "slug")]
        public string Slug { get; set; }

        [DataMember(Name = "show")]
        public TraktShowDataContract Show { get; set; }

        [DataMember(Name = "movie")]
        public TraktMovieDataContract Movie { get; set; }

        [DataMember(Name = "privacy")]
        public string Privacy { get; set; }
    }



    [DataContract]
    public class TraktUserListDataContract
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "slug")]
        public string Slug { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "privacy")]
        public string Privacy { get; set; }

        [DataMember(Name = "show_numbers")]
        public bool ShowNumbers { get; set; }

        [DataMember(Name = "allow_shouts")]
        public bool AllowShouts { get; set; }
    }
}
