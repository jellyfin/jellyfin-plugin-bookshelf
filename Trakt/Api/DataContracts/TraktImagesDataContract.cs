using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts
{
    [DataContract]
    public class TraktImagesDataContract
    {
        [DataMember(Name = "poster")]
        public string PosterUrl { get; set; }

        [DataMember(Name = "fanart")]
        public string FanartUrl { get; set; }

        [DataMember(Name = "banner")]
        public string BannerUrl { get; set; }

        [DataMember(Name = "screen")]
        public string ScreenUrl { get; set; }
    }
}
