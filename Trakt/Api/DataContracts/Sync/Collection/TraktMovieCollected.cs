using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Sync.Collection
{
    [DataContract]
    public class TraktMovieCollected : TraktMovie
    {
        [DataMember(Name = "collected_at", EmitDefaultValue = false)]
        public string CollectedAt { get; set; }

        [DataMember(Name = "media_type", EmitDefaultValue = false)]
        public string MediaType { get; set; }

        [DataMember(Name = "resolution", EmitDefaultValue = false)]
        public string Resolution { get; set; }

        [DataMember(Name = "audio", EmitDefaultValue = false)]
        public string Audio { get; set; }

        [DataMember(Name = "audio_channels", EmitDefaultValue = false)]
        public string AudioChannels { get; set; }

        [DataMember(Name = "3d", EmitDefaultValue = false)]
        public bool Is3D { get; set; }
    }
}