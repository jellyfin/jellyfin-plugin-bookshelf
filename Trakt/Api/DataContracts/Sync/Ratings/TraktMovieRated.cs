using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Sync.Ratings
{
    [DataContract]
    public class TraktMovieRated : TraktRated
    {
        [DataMember(Name = "title", EmitDefaultValue = false)]
        public string Title { get; set; }

        [DataMember(Name = "year", EmitDefaultValue = false)]
        public int? Year { get; set; }

        [DataMember(Name = "ids")]
        public TraktMovieId Ids { get; set; }
    }
}