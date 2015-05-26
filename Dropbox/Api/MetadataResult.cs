using System.Collections.Generic;

namespace Dropbox.Api
{
    public class MetadataResult
    {
        public string path { get; set; }
        public bool is_dir { get; set; }
        public string mime_type { get; set; }
    }
}
