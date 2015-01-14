using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Users.Ratings
{
    [DataContract]
    public class TraktSeasonRated : TraktRated
    {
        [DataMember(Name = "season")]
        public TraktSeason Season { get; set; }
    }
}