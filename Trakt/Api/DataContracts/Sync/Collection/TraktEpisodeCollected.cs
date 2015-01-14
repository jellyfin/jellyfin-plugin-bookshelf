using System.Collections.Generic;
using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Sync.Collection
{
    [DataContract]
    public class TraktEpisodeCollected : TraktEpisode
    {
        [DataMember(Name = "collected_at")]
        public string CollectedAt { get; set; }

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