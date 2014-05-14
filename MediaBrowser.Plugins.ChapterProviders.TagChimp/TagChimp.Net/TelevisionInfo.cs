using System;
using System.Xml.Serialization;

namespace TagChimp
{
    public class TelevisionInfo
    {
        [XmlElement("showName")]
        public string ShowName { get; set; }
        [XmlElement("season")]
        public string Season { get; set; }

        private string locked;
        [XmlElement("seasonLocked")]
        public string LockedString
        {
            get { return locked; }
            set
            {
                locked = value;
                if (!String.IsNullOrEmpty(value))
                {
                    Locked = value.ToLower() == "yes";
                }
            }
        }

        [XmlIgnore]
        public bool Locked { get; set; }
        [XmlElement("episode")]
        public int Episode { get; set; }
        [XmlElement("episodeID")]
        public string EpisodeId { get; set; }
        [XmlElement("productionCode")]
        public string ProductionCode { get; set; }
        [XmlElement("network")]
        public string Network { get; set; }
    }
}