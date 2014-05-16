using System.Configuration;

namespace TagChimp
{
    public enum SearchType
    {
        Search,
        Lookup
    }

    public class SearchParameters
    {
        private string token = "12222029995372A1980D08F";
        public string Token
        {
            get { return token; }
            set { token = value; }
        }
        public SearchType Type { get; set; }
        public string Title { get; set; }

        public string TotalChapters { get; set; }
        public int? Id { get; set; }
        public string Language { get; set; }
        public string UserId { get; set; }
        public int? Limit { get; set; }
        public bool? Locked { get; set; }
        public string Asin { get; set; }
        public string ImdbId { get; set; }
        public string NetflixId { get; set; }
        public string Gtin { get; set; }
        public string Show { get; set; }
        public int? Season { get; set; }
        public int? Episode { get; set; }
        public string VideoKind { get; set; }
    }
}