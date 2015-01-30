using System.Collections.Generic;
using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Sync.Collection
{
    [DataContract]
    public class TraktShowCollected : TraktShow
    {
        [DataMember(Name = "seasons")]
        public List<TraktSeasonCollected> Seasons { get; set; }

        [DataContract]
        public class TraktSeasonCollected
        {
            [DataMember(Name = "number")]
            public int Number { get; set; }

            [DataMember(Name = "episodes")]
            public List<TraktEpisodeCollected> Episodes { get; set; }
        }
    }
}