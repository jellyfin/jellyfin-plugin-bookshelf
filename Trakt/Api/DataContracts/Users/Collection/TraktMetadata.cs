using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts.Users.Collection
{
    [DataContract]
    public class TraktMetadata
    {
        [DataMember(Name = "media_type")]
        public string MediaType { get; set; }

        [DataMember(Name = "resolution")]
        public string Resolution { get; set; }

        [DataMember(Name = "audio")]
        public string Audio { get; set; }

        [DataMember(Name = "audio_channels")]
        public string AudioChannels { get; set; }

        [DataMember(Name = "3d")]
        public bool Is3D { get; set; }
    }
}