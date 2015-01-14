using System.Collections.Generic;
using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Sync.Watched
{
    public class TraktShowWatched : TraktShow
    {
        [DataMember(Name = "watched_at")]
        public string WatchedAt { get; set; }

        [DataMember(Name = "seasons")]
        public List<TraktSeasonWatched> Seasons { get; set; }
    }
}