using System.Collections.Generic;
using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Sync.Ratings
{
    [DataContract]
    public class TraktShowRated : TraktRated
    {
        [DataMember(Name = "title", EmitDefaultValue = false)]
        public string Title { get; set; }

        [DataMember(Name = "year", EmitDefaultValue = false)]
        public int? Year { get; set; }

        [DataMember(Name = "ids")]
        public TraktShowId Ids { get; set; }

        [DataMember(Name = "seasons")]
        public List<TraktSeasonRated> Seasons { get; set; }

        public class TraktSeasonRated : TraktRated
        {
            [DataMember(Name = "number")]
            public int? Number { get; set; }

            [DataMember(Name = "episodes")]
            public List<TraktEpisodeRated> Episodes { get; set; }
        }
    }
}