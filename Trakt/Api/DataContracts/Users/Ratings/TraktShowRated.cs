using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Users.Ratings
{
    [DataContract]
    public class TraktShowRated : TraktRated
    {
        [DataMember(Name = "show")]
        public TraktShow Show { get; set; }
    }
}