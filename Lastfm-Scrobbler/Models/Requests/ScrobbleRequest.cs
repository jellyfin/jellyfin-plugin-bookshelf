namespace LastfmScrobbler.Models.Requests
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class ScrobbleRequest : BaseAuthedRequest
    {
        public string Track     { get; set; }
        public string Album     { get; set; }
        public string Artist    { get; set; }
        public int    Timestamp { get; set; }

        public override Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(base.ToDictionary()) 
            {
                { "track",     Track },
                { "album",     Album },
                { "artist",    Artist },
                { "timestamp", Timestamp.ToString() }
            };
        }
    }
}
