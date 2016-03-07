namespace LastfmScrobbler.Models.Requests
{
    using System.Collections.Generic;

    public class GetLovedTracksRequest : BaseRequest
    {
        public string User { get; set; }

        public override Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>(base.ToDictionary()) 
            {
                { "user", User }
            };
        }
    }
}
