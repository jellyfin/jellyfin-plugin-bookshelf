using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts
{
    [DataContract]
    public class TraktResponseDataContract
    {
        [DataMember(Name = "status")]
        public string Status { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "rating")]
        public string Rating { get; set; }

        [DataMember(Name = "ratings")]
        public TraktRatingsDataContract Ratings { get; set; }

        [DataMember(Name = "facebook")]
        public bool Facebook { get; set; }

        [DataMember(Name = "twitter")]
        public bool Twitter { get; set; }

        [DataMember(Name = "tumblr")]
        public bool Tumblr { get; set; }
    }
}
