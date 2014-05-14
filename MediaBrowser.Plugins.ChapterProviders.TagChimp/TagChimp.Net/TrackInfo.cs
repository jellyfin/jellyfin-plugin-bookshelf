using System.Xml.Serialization;

namespace TagChimp
{
    public class TrackInfo
    {
        [XmlElement("trackNum")]
        public string TrackNum { get; set; }
        [XmlElement("trackTotal")]
        public string TrackTotal { get; set; }
    }
}