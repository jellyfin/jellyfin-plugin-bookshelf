using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts.BaseModel
{
    [DataContract]
    public class TraktEpisodeId : TraktId
    {
        [DataMember(Name = "imdb", EmitDefaultValue = false)]
        public string Imdb { get; set; }

        [DataMember(Name = "tmdb", EmitDefaultValue = false)]
        public int? Tmdb { get; set; }

        [DataMember(Name = "tvdb", EmitDefaultValue = false)]
        public int? Tvdb { get; set; }

        [DataMember(Name = "tvrage", EmitDefaultValue = false)]
        public int? TvRage { get; set; }
    }
}