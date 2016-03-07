namespace LastfmScrobbler.Models.Responses
{
    using System.Runtime.Serialization;

    [DataContract]
    public class ScrobbleResponse : BaseResponse
    {
        [DataMember(Name = "scrobbles")]
        public Scrobbles Scrobbles { get; set; }
    }
}
