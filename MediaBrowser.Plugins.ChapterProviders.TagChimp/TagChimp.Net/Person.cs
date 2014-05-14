using System.Xml.Serialization;

namespace TagChimp
{
    public class Person
    {
        [XmlText]
        public string FullName { get; set; }
    }
}