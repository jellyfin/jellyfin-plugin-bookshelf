using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts
{
    [DataContract]
    public class TraktFriendDataContract
    {
        [DataMember(Name = "username")]
        public string Username { get; set; }

        [DataMember(Name = "protected")]
        public bool Protected { get; set; }

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

        [DataMember(Name = "avatar")]
        public string Avatar { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "vip")]
        public bool Vip { get; set; }

        [DataMember(Name = "approved")]
        public int Approved { get; set; }

        [DataMember(Name = "requested")]
        public int Requested { get; set; }

        [DataMember(Name = "watching")]
        public Activity Watching { get; set; }

        [DataMember(Name = "watched")]
        public List<Activity> Watched { get; set; }
    }
}
