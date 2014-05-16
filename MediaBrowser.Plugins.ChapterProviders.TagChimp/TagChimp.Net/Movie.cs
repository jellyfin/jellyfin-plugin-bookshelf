using System;
using System.Xml.Serialization;

namespace TagChimp
{
    public class Movie
    {
        public Movie()
        {
            MovieTags = new MovieTags();
            Chapters = new ChapterCollection();
        }

        [XmlElement("language")]
        public string Language { get; set; }

        private string locked;
        [XmlElement("locked")]
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
        [XmlElement("tagChimpID")]
        public int TagChimpId { get; set; }
        [XmlElement("amazonASIN")]
        public string AmazonASIN { get; set; }
        [XmlElement("imdbID")]
        public string ImdbID { get; set; }
        [XmlElement("netflixID")]
        public string NetflixId { get; set; }
        [XmlElement("iTunesURL")]
        public string iTunesUrl { get; set; }
        [XmlElement("gtin")]
        public string Gtin { get; set; }
        [XmlElement("movieTags")]
        public MovieTags MovieTags { get; set; }
        [XmlElement("movieChapters")]
        public ChapterCollection Chapters { get; set; }
    }
}