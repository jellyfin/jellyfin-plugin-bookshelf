using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Sync.Watched
{
    public class TraktMovieWatched : TraktMovie
    {
        [DataMember(Name = "watched_at")]
        public string WatchedAt { get; set; }
    }
}