using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts.BaseModel
{
    [DataContract]
    public abstract class TraktRated
    {
        [DataMember(Name = "rating")]
        public int? Rating { get; set; }

        [DataMember(Name = "rated_at")]
        public string RatedAt { get; set; }
    }
}