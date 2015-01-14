using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Users.Ratings
{
    [DataContract]
    public class TraktEpisodeRated : TraktRated
    {
        [DataMember(Name = "episode")]
        public TraktEpisode Episode { get; set; }
    }
}