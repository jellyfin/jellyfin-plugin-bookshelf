using System.Xml.Serialization;

namespace TagChimp
{
    public class Genre
    {
        [XmlText]
        public string Name { get; set; }
    }
}