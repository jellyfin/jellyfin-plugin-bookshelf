using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Users.Ratings
{
    [DataContract]
    public class TraktMovieRated : TraktRated
    {
        [DataMember(Name = "movie")]
        public TraktMovie Movie { get; set; }
    }
}