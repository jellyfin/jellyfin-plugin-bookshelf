using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts.BaseModel
{
    [DataContract]
    public class TraktSeason
    {
        [DataMember(Name = "number", EmitDefaultValue = false)]
        public int? Number { get; set; }

        [DataMember(Name = "ids")]
        public TraktSeasonId Ids { get; set; }
    }
}