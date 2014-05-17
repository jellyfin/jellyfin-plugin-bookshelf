using System.Collections.Generic;
using System.Xml.Serialization;

namespace TagChimp
{
    public class ErrorMessage
    {
        [XmlElement("error")]
        public string Error { get; set; }
    }

    public class TagChimpException : System.Exception
    {
        public TagChimpException(string message, string url, string xml)
            : base(message)
        {
            RequestUrl = url;
            XmlResponse = xml;
        }

        public string RequestUrl { get; set; }
        public string XmlResponse { get; set; }
    }

    [XmlRoot("items")]
    public class SearchResults
    {
        public SearchResults()
        {
            Movies = new List<Movie>();
            Message = new ErrorMessage();
        }

        [XmlElement("totalResults")]
        public int TotalResults { get; set; }

        [XmlElement("movie")]
        public List<Movie> Movies { get; set; }

        [XmlElement("message")]
        public ErrorMessage Message { get; set; }
    }
}