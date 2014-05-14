using System.Xml.Serialization;

namespace TagChimp
{
    public class MovieTags
    {
        public MovieTags()
        {
            MovieInfo = new MovieInfo();
            TelevisionInfo = new TelevisionInfo();
            SortingInfo = new SortingInfo();
            TrackInfo = new TrackInfo();
        }

        [XmlElement("info")]
        public MovieInfo MovieInfo { get; set; }
        [XmlElement("television")]
        public TelevisionInfo TelevisionInfo { get; set; }
        [XmlElement("sorting")]
        public SortingInfo SortingInfo { get; set; }
        [XmlElement("track")]
        public TrackInfo TrackInfo { get; set; }
        [XmlElement("coverArtLarge")]
        public string CoverArtLarge { get; set; }
        [XmlElement("coverArtSmall")]
        public string CoverArtSmall { get; set; }
    }
}