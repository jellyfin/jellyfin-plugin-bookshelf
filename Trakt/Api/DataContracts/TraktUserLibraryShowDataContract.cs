using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts
{

    /// <summary>
    /// Used for api calls to user/library/shows/collection
    /// http://trakt.tv/api-docs/user-library-shows-collection
    /// 
    /// and also for api calls to user/library/shows/watched
    /// http://trakt.tv/api-docs/user-library-shows-watched
    /// </summary>
    [DataContract]
    public class TraktUserLibraryShowDataContract
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "year")]
        public int Year { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "imdb_id")]
        public string ImdbId { get; set; }

        [DataMember(Name = "tvdb_id")]
        public string TvdbId { get; set; }

        [DataMember(Name = "tvrage_id")]
        public string TvRageId { get; set; }

        [DataMember(Name = "images")]
        public TraktImagesDataContract Images { get; set; }
        
        [DataMember(Name = "genres")]
        public List<string> Genres { get; set; }
        
        [DataMember(Name = "seasons")]
        public List<TraktSeasonDataContract> Seasons { get; set; }


        [DataContract]
        public class TraktSeasonDataContract
        {
            [DataMember(Name = "season")]
            public int Season { get; set; }

            [DataMember(Name = "episodes")]
            public List<int> Episodes { get; set; }
        }
    }
}
