using System.Collections.Generic;
using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Sync.Watched
{
    [DataContract]
    public class TraktSeasonWatched : TraktSeason
    {
        [DataMember(Name = "watched_at", EmitDefaultValue = false)]
        public string WatchedAt { get; set; }

        [DataMember(Name = "episodes")]
        public List<TraktEpisodeWatched> Episodes { get; set; }
    }
}
