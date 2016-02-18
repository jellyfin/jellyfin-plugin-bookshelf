namespace LastfmScrobbler.Models
{
    using System.Runtime.Serialization;

    //Wow what a bad response object!
    [DataContract]
    public class Scrobbles
    {
        [DataMember(Name = "@attr")]
        public ScrobbleAttributes Attributes { get; set; }
    }

    [DataContract]
    public class ScrobbleAttributes
    {
        [DataMember(Name = "accepted")]
        public bool Accepted { get; set; }

        [DataMember(Name = "ignored")]
        public bool Ignored { get; set; }
    }
}
