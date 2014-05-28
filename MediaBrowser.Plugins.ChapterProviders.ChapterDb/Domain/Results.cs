using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MediaBrowser.Plugins.ChapterProviders.ChapterDb.Domain {

    [XmlRoot("results")]
    public class Results
    {
        [XmlElement("chapterInfo")]
        public Detail Detail { get; set; }
    }

    public class Detail
    {
        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlAttribute("extractor")]
        public string Extractor { get; set; }

        [XmlAttribute("client")]
        public string Client { get; set; }

        [XmlAttribute("confirmations")]
        public int Confirmations { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("ref")]
        public Ref Ref { get; set; }

        [XmlElement("source")]
        public Source Source { get; set; }

        [XmlElement("chapters")]
        public IList<Chapter> Chapters { get; set; }

        [XmlElement("createdBy")]
        public string CreatedBy { get; set; }

        [XmlElement("createdDate")]
        public string createdDate { get; set; }
    }

    public class Ref
    {
        [XmlElement("chapterSetId")]
        public int ChapterSetId { get; set; }
    }

    public class Source
    {
        [XmlElement("type")]
        public string Type { get; set; }

        [XmlElement("hash")]
        public string Hash { get; set; }

        [XmlElement("fps")]
        public string Fps { get; set; }

        [XmlElement("duration")]
        public TimeSpan Duration { get; set; }
    }

    public class Chapter
    {
        [XmlAttribute("time")]
        public TimeSpan Time { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }
    }
}
