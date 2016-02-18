namespace LastfmScrobbler.Models.Requests
{
    using System.Collections.Generic;

    public class TrackLoveRequest : BaseAuthedRequest
    {
        public string Track  { get; set; }
        public string Artist { get; set; }

        public override Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(base.ToDictionary())
            {
                { "track" , Track  },
                { "artist", Artist }
            };
        }
    }
}
