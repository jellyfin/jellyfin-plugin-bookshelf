using System.Collections.Generic;
using System.Xml.Serialization;

namespace TagChimp
{
    public class MovieInfo
    {
        public MovieInfo()
        {
            Directors = new List<Person>();
            Producers = new List<Person>();
            ScreenWriters = new List<Person>();
            Cast = new List<Person>();
            Artists = new List<Person>();
            AdditionalGenres = new List<Genre>();
        }

        [XmlElement("kind")]
        public string Kind { get; set; }
        [XmlElement("movieTitle")]
        public string Title { get; set; }
        [XmlElement("releaseDate")]
        public string ReleaseDate { get; set; }
        [XmlElement("releaseDateY")]
        public string ReleaseDateYear { get; set; }
        [XmlElement("releaseDateM")]
        public string ReleaseDateMonth { get; set; }
        [XmlElement("releaseDateD")]
        public string ReleaseDateDay { get; set; }
        [XmlElement("genre")]
        public Genre Genre { get; set; }
        [XmlArray("additionalGenres"), XmlArrayItem("genre")]
        public List<Genre> AdditionalGenres { get; set; }
        [XmlElement("rating")]
        public string Rating { get; set; }
        [XmlElement("shortDescription")]
        public string ShortDescription { get; set; }
        [XmlElement("longDescription")]
        public string LongDescription { get; set; }
        [XmlElement("advisory")]
        public string Advisory { get; set; }
        [XmlElement("copyright")]
        public string Copyright { get; set; }
        [XmlElement("comments")]
        public string Comments { get; set; }

        [XmlArray("directors"), XmlArrayItem("director")]
        public List<Person> Directors { get; set; }

        [XmlArray("producers"), XmlArrayItem("producer")]
        public List<Person> Producers { get; set; }

        [XmlArray("screenwriters"), XmlArrayItem("screenwriter")]
        public List<Person> ScreenWriters { get; set; }

        [XmlArray("cast"), XmlArrayItem("actor")]
        public List<Person> Cast { get; set; }

        [XmlArray("artist"), XmlArrayItem("artistName")]
        public List<Person> Artists { get; set; }
    }
}