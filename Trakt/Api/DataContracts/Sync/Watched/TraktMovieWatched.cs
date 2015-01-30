using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Sync.Watched
{
    [DataContract]
    public class TraktMovieWatched : TraktMovie
    {
        [DataMember(Name = "watched_at", EmitDefaultValue = false)]
        public string WatchedAt { get; set; }
    }
}