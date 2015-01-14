using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Users.Collection
{
    [DataContract]
    public class TraktMovieCollected
    {
        [DataMember(Name = "collected_at")]
        public string CollectedAt { get; set; }

        [DataMember(Name = "metadata")]
        public TraktMetadata Metadata { get; set; }

        [DataMember(Name = "movie")]
        public TraktMovie Movie { get; set; }
    }
}