using System;
using System.Collections.Generic;

namespace OneDrive.Api
{
    public class UploadSession
    {
        public string uploadUrl { get; set; }
        public DateTime expirationDateTime { get; set; }
        public List<string> nextExpectedRanges { get; set; }
        public string id { get; set; }
    }
}
