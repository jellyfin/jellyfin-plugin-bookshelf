using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts.BaseModel
{
    [DataContract]
    public class TraktEpisode
    {
        [DataMember(Name = "season", EmitDefaultValue = false)]
        public int? Season { get; set; }

        [DataMember(Name = "number", EmitDefaultValue = false)]
        public int? Number { get; set; }

        [DataMember(Name = "title", EmitDefaultValue = false)]
        public string Title { get; set; }

        [DataMember(Name = "ids")]
        public TraktEpisodeId Ids { get; set; }
    }
}