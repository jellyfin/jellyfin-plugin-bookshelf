using System.Collections.Generic;

namespace Dropbox.Api
{
    public class DeltaResult
    {
        public List<DeltaEntry> entries { get; set; }
        public bool reset { get; set; }
        public string cursor { get; set; }
        public bool has_more { get; set; }
    }
}
