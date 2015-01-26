using System.Runtime.Serialization;
using Trakt.Api.DataContracts.BaseModel;

namespace Trakt.Api.DataContracts.Scrobble
{
    [DataContract]
    public class TraktScrobbleEpisode
    {
        [DataMember(Name = "show", EmitDefaultValue = false)]
        public TraktShow Show { get; set; }

        [DataMember(Name = "episode")]
        public TraktEpisode Episode { get; set; }

        [DataMember(Name = "progress")]
        public float Progress { get; set; }

        [DataMember(Name = "app_version")]
        public string AppVersion { get; set; }

        [DataMember(Name = "app_date")]
        public string AppDate { get; set; }
    }
}