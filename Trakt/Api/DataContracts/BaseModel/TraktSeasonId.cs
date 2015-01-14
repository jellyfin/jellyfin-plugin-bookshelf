using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts.BaseModel
{
    [DataContract]
    public class TraktSeasonId : TraktId
    {
        [DataMember(Name = "tmdb")]
        public int? Tmdb { get; set; }

        [DataMember(Name = "tvdb")]
        public int? Tvdb { get; set; }

        [DataMember(Name = "tvrage")]
        public int? TvRage { get; set; }
    }
}