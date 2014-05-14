using System.Xml.Serialization;

namespace TagChimp
{
    public class SortingInfo
    {
        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("albumArtist")]
        public string AlbumArtist { get; set; }
        [XmlElement("album")]
        public string Album { get; set; }
        [XmlElement("show")]
        public string Show { get; set; }
    }
}