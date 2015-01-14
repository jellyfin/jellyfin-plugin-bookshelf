using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Users.Watched
{
    [DataContract]
    public class TraktMovieWatched
    {
        [DataMember(Name = "plays")]
        public int Plays { get; set; }

        [DataMember(Name = "last_watched_at")]
        public string LastWatchedAt { get; set; }

        [DataMember(Name = "movie")]
        public TraktMovie Movie { get; set; }
    }
}