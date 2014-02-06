using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.NextPvr.Responses
{
    public class CancelRecordingResponse
    {
        public bool CanceledRecordingError(Stream stream, IJsonSerializer json)
        {
            var root = json.DeserializeFromStream<RootObject>(stream);

            if (root.epgEventJSONObject != null && root.epgEventJSONObject.rtn != null)
            {
                return root.epgEventJSONObject.rtn.Error;
            }
            throw new ApplicationException("Failed to cancel the recording.");
        }

        public class Rtn
        {
            public bool Error { get; set; }
            public int HTTPStatus { get; set; }
            public string Message { get; set; }
        }

        public class EpgEventJSONObject
        {
            public Rtn rtn { get; set; }
        }

        public class RootObject
        {
            public EpgEventJSONObject epgEventJSONObject { get; set; }
        }
    }
}
