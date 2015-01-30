using System.Collections.Generic;
using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Sync.Watched
{
    [DataContract]
    public class TraktShowWatched : TraktShow
    {
        [DataMember(Name = "watched_at", EmitDefaultValue = false)]
        public string WatchedAt { get; set; }

        [DataMember(Name = "seasons")]
        public List<TraktSeasonWatched> Seasons { get; set; }
    }
}