using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace MediaBrowser.Plugins.ChapterProviders.ChapterDb.Domain
{

    [XmlRoot("results")]
    public class Results
    {
        [XmlElement("chapterInfo", Namespace = "http://jvance.com/2008/ChapterGrabber")]
        public List<Detail> Detail { get; set; }

        public Results()
        {
            Detail = new List<Detail>();
        }
    }

    [XmlRoot("chapterInfo", Namespace = "http://jvance.com/2008/ChapterGrabber")]
    public class Detail
    {
        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlAttribute("extractor")]
        public string Extractor { get; set; }

        [XmlAttribute("client")]
        public string Client { get; set; }

        [XmlAttribute("confirmations")]
        public string Confirmations { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("ref")]
        public Ref Ref { get; set; }

        [XmlElement("source")]
        public Source Source { get; set; }

        [XmlElement("chapters")]
        public ChapterCollection ChapterCollection { get; set; }

        [XmlElement("createdBy")]
        public string CreatedBy { get; set; }

        [XmlElement("createdDate")]
        public string CreatedDate { get; set; }

        [XmlElement("updateBy")]
        public string UpdatedBy { get; set; }

        [XmlElement("updatedDate")]
        public string UpdatedDate { get; set; }

        public Detail()
        {
            Ref = new Ref();
            Source = new Source();
            ChapterCollection = new ChapterCollection();
        }
    }

    public class Ref
    {
        [XmlElement("chapterSetId")]
        public string ChapterSetId { get; set; }
    }

    public class Source
    {
        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("type")]
        public string Type { get; set; }

        [XmlElement("hash")]
        public string Hash { get; set; }

        [XmlElement("fps")]
        public string Fps { get; set; }

        [XmlIgnore]
        public TimeSpan Duration { get; set; }

        private string duration;

        [XmlElement("duration")]
        public string DurationString
        {
            get { return duration; }
            set
            {
                duration = value;
                Duration = Helper.ParseTime(value);
            }
        }

        public Source()
        {
            Duration = TimeSpan.Zero;
        }
    }

    public class ChapterCollection
    {
        [XmlElement("chapter")]
        public List<Chapter> Chapters { get; set; }

        public ChapterCollection()
        {
            Chapters = new List<Chapter>();
        }
    }

    public class Chapter
    {
        [XmlIgnore]
        public TimeSpan Time { get; set; }

        private string time;

        [XmlAttribute("time")]
        public string TimeString
        {
            get { return time; }
            set
            {
                time = value;
                Time = Helper.ParseTime(value);
            }
        }

        [XmlAttribute("name")]
        public string Name { get; set; }

        public Chapter()
        {
            Time = TimeSpan.Zero;
        }
    }

    public static class Helper
    {
        public static TimeSpan ParseTime(string time)
        {
            if (!String.IsNullOrEmpty((time)))
            {
                TimeSpan val;
                if (TimeSpan.TryParse(time, out val))
                {
                    return val;
                }
            }
            return TimeSpan.Zero;
        }

    }
}