using System.Collections.Generic;
using Newtonsoft.Json;

namespace OneDrive.Api
{
    public class ViewChangesResult
    {
        public List<ViewChange> value { get; set; }

        [JsonProperty("@odata.nextLink")]
        public string NextLink { get; set; }

        [JsonProperty("@changes.hasMoreChanges")]
        public bool HasMoreChanges { get; set; }

        [JsonProperty("@changes.token")]
        public string Token { get; set; }
    }
}
