using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts.BaseModel
{
    [DataContract]
    public class TraktMovieId : TraktId
    {
        [DataMember(Name = "imdb")]
        public string Imdb { get; set; }

        [DataMember(Name = "tmdb")]
        public int? Tmdb { get; set; }
    }
}