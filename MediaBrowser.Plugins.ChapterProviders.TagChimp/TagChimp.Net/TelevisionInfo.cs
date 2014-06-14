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
        public bool Locked { get; private set; }

        private string episode;
        [XmlElement("episode")]
        public string EpisodeString {
            get { return episode; }
            set {
                episode = value;
                if (!String.IsNullOrEmpty(value)) {
                    Episode = Convert.ToInt32(value);
                }
            }
        }

        [XmlIgnore]
        public int Episode { get; private set; }

        [XmlElement("episodeID")]
        public string EpisodeId { get; set; }
        [XmlElement("productionCode")]
        public string ProductionCode { get; set; }
        [XmlElement("network")]
        public string Network { get; set; }
    }
}