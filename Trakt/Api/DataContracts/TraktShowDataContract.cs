using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts
{
    [DataContract]
    public class TraktShowDataContract
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "year")]
        public int Year { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "first_aired")]
        public int FirstAired { get; set; }

        [DataMember(Name = "country")]
        public string Country { get; set; }

        [DataMember(Name = "overview")]
        public string Overview { get; set; }

        [DataMember(Name = "runtime")]
        public int Runtime { get; set; }

        [DataMember(Name = "network")]
        public string Network { get; set; }

        [DataMember(Name = "air_day")]
        public string AirDay { get; set; }

        [DataMember(Name = "air_time")]
        public string AirTime { get; set; }

        [DataMember(Name = "certification")]
        public string Certification { get; set; }

        [DataMember(Name = "imdb_id")]
        public string ImdbId { get; set; }

        [DataMember(Name = "tvdb_id")]
        public string TvdbId { get; set; }

        [DataMember(Name = "tvrage_id")]
        public string TvRageId { get; set; }

        [DataMember(Name = "inserted")]
        public int Inserted { get; set; }

        [DataMember(Name = "images")]
        public TraktImagesDataContract Images { get; set; }

        [DataMember(Name = "watchers")]
        public int Watchers { get; set; }

        [DataMember(Name = "ratings")]
        public TraktRatingsDataContract Ratings { get; set; }

        [DataMember(Name = "rating")]
        public string Rating { get; set; }

        [DataMember(Name = "in_watchlist")]
        public bool InWatchlist { get; set; }

        [DataMember(Name = "genres")]
        public List<string> Genres { get; set; }

        [DataMember(Name = "episodes")]
        public List<TraktEpisodeDataContract> Episodes { get; set; }

        [DataMember(Name = "people")]
        public TraktPeopleDataContract People { get; set; }
    }
}
