namespace LastfmScrobbler.Models.Responses
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class LovedTracksResponse : BaseResponse
    {
        [DataMember(Name="lovedtracks")]
        public LovedTracks LovedTracks { get; set; }

        public bool HasLovedTracks()
        {
            return LovedTracks != null && LovedTracks.Tracks != null && LovedTracks.Tracks.Count > 0;
        }
    }

    [DataContract]
    public class LovedTracks
    {
        [DataMember(Name = "track")]
        public List<LastfmLovedTrack> Tracks { get; set; }
    }
}
