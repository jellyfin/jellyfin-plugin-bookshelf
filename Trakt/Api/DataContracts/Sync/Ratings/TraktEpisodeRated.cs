using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Sync.Ratings
{
    [DataContract]
    public class TraktEpisodeRated : TraktRated
    {
        [DataMember(Name = "number", EmitDefaultValue = false)]
        public int? Number { get; set; }

        [DataMember(Name = "ids")]
        public TraktEpisodeId Ids { get; set; }
    }
}