using System.Collections.Generic;
using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Users.Collection
{
    [DataContract]
    public class TraktShowCollected
    {
        [DataMember(Name = "last_collected_at")]
        public string LastCollectedAt { get; set; }

        [DataMember(Name = "show")]
        public TraktShow Show { get; set; }

        [DataMember(Name = "seasons")]
        public List<TraktSeasonCollected> Seasons { get; set; }

        [DataContract]
        public class TraktSeasonCollected
        {
            [DataMember(Name = "number")]
            public int Number { get; set; }

            [DataMember(Name = "episodes")]
            public List<TraktEpisodeCollected> Episodes { get; set; }

            [DataContract]
            public class TraktEpisodeCollected
            {
                [DataMember(Name = "number")]
                public int Number { get; set; }

                [DataMember(Name = "collected_at")]
                public string CollectedAt { get; set; }

                [DataMember(Name = "metadata")]
                public TraktMetadata Metadata { get; set; }
            }
        }
    }
}