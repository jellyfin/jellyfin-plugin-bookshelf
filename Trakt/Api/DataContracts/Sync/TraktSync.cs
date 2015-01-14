using System.Collections.Generic;
using System.Runtime.Serialization;
using Trakt.Api.DataContracts.Sync.Collection;
using Trakt.Api.DataContracts.Sync.Ratings;
using Trakt.Api.DataContracts.Sync.Watched;

namespace Trakt.Api.DataContracts.Sync
{
    [DataContract]
    public class TraktSync<TMovie, TShow, TEpisode>
    {
        [DataMember(Name = "movies", EmitDefaultValue = false)]
        public List<TMovie> Movies { get; set; }

        [DataMember(Name = "shows", EmitDefaultValue = false)]
        public List<TShow> Shows { get; set; }

        [DataMember(Name = "episodes", EmitDefaultValue = false)]
        public List<TEpisode> Episodes { get; set; }
    }

    [DataContract]
    public class TraktSyncRated : TraktSync<TraktMovieRated, TraktShowRated, TraktEpisodeRated>
    {
    }

    [DataContract]
    public class TraktSyncWatched : TraktSync<TraktMovieWatched, TraktShowWatched, TraktEpisodeWatched>
    {
    }

    [DataContract]
    public class TraktSyncCollected : TraktSync<TraktMovieCollected, TraktShowCollected, TraktEpisodeCollected>
    {
    }
}