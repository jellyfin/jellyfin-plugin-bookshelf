namespace LastfmScrobbler.Models
{
    using System.Runtime.Serialization;

    [DataContract]
    public class BaseLastfmTrack
    {
        [DataMember(Name="artist")]
        public LastfmArtist Artist { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "mbid")]
        public string MusicBrainzId { get; set; }
    }

    [DataContract]
    public class LastfmArtist
    {
        [DataMember(Name="name")]
        public string Name { get; set; }

        [DataMember(Name = "mbid")]
        public string MusicBrainzId { get; set; }
    }

    public class LastfmLovedTrack : BaseLastfmTrack
    {
    }

    [DataContract]
    public class LastfmTrack : BaseLastfmTrack
    {
        [DataMember(Name="playcount")]
        public int PlayCount { get; set; }
    }
}
