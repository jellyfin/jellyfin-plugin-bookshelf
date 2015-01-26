using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts.BaseModel
{
    [DataContract]
    public class TraktMovieId : TraktId
    {
        [DataMember(Name = "imdb", EmitDefaultValue = false)]
        public string Imdb { get; set; }

        [DataMember(Name = "tmdb", EmitDefaultValue = false)]
        public int? Tmdb { get; set; }
    }
}