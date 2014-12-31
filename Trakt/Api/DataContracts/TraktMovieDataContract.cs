using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts
{
    [DataContract]
    public class TraktMovieDataContract
    {

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "year")]
        public int Year { get; set; }

        [DataMember(Name = "released")]
        public int Released { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "trailer")]
        public string Trailer { get; set; }

        [DataMember(Name = "runtime")]
        public int Runtime { get; set; }

        [DataMember(Name = "tagline")]
        public string Tagline { get; set; }

        [DataMember(Name = "overview")]
        public string Overview { get; set; }

        [DataMember(Name = "certification")]
        public string Certification { get; set; }

        [DataMember(Name = "imdb_id")]
        public string ImdbId { get; set; }

        [DataMember(Name = "tmdb_id")]
        public string TmdbId { get; set; }

        [DataMember(Name = "images")]
        public TraktImagesDataContract Images { get; set; }

        [DataMember(Name = "watchers")]
        public int Watchers { get; set; }

        [DataMember(Name = "ratings")]
        public TraktRatingsDataContract Ratings { get; set; }

        [DataMember(Name = "genres")]
        public List<string> Genres { get; set; }

        [DataMember(Name = "plays")]
        public int Plays { get; set; }

        [DataMember(Name = "last_played")]
        public long LastPlayed { get; set; }

        [DataMember(Name = "unseen")]
        public bool Unseen { get; set; }

        [DataMember(Name = "watched")]
        public bool Watched { get; set; }

        [DataMember(Name = "in_collection")]
        public bool InCollection { get; set; }

        [DataMember(Name = "in_watchlist")]
        public bool InWatchlist { get; set; }

        [DataMember(Name = "rating")]
        public string Rating { get; set; }

        [DataMember(Name = "people")]
        public TraktPeopleDataContract People { get; set; }
    }




}
