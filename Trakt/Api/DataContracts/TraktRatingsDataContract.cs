using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts
{
    [DataContract]
    public class TraktRatingsDataContract
    {
        [DataMember(Name = "percentage")]
        public int Percentage { get; set; }

        [DataMember(Name = "votes")]
        public int Votes { get; set; }

        [DataMember(Name = "loved")]
        public int Loved { get; set; }

        [DataMember(Name = "hated")]
        public int Hated { get; set; }
    }
}
