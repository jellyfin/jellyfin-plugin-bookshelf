namespace LastfmScrobbler.Models
{
    using System.Runtime.Serialization;

    [DataContract]
    public class MobileSession
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "key")]
        public string Key { get; set; }

        [DataMember(Name = "subscriber")]
        public int Subscriber { get; set; }
    }
}
