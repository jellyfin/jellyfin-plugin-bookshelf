using Newtonsoft.Json;

namespace OneDrive.Api
{
    public class CreateFolderParameters
    {
        public string name { get; set; }
        public object folder { get; set; }

        [JsonProperty("@name.conflictBehavior")]
        public string conflictBehavior { get; set; }
    }
}
