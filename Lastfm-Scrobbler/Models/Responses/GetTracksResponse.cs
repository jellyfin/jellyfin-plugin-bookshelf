namespace LastfmScrobbler.Models.Responses
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class GetTracksResponse : BaseResponse
    {
        [DataMember(Name="tracks")]
        public GetTracksTracks Tracks { get; set; }

        public bool HasTracks()
        {
            return Tracks != null && Tracks.Tracks != null && Tracks.Tracks.Count > 0;
        }
    }

    [DataContract]
    public class GetTracksTracks
    {
        [DataMember(Name="track")]
        public List<LastfmTrack> Tracks { get; set; }

        [DataMember(Name = "@attr")]
        public GetTracksMeta Metadata { get; set; }
    }

    [DataContract]
    public class GetTracksMeta
    {
        [DataMember(Name = "totalPages")]
        public int TotalPages { get; set; }

        [DataMember(Name = "total")]
        public int TotalTracks { get; set; }

        [DataMember(Name = "page")]
        public int Page { get; set; }

        public bool IsLastPage()
        {
            return Page.Equals(TotalPages);
        }
    }
}
