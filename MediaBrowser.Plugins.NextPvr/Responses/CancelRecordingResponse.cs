using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.NextPvr.Responses
{
    public class CancelDeleteRecordingResponse
    {
        public bool? RecordingError(Stream stream, IJsonSerializer json,ILogger logger)
        {
            var root = json.DeserializeFromStream<RootObject>(stream);

            if (root.epgEventJSONObject != null && root.epgEventJSONObject.rtn != null)
            {
                logger.Debug("[NextPvr] RecordingError Response: {0}", json.SerializeToString(root));
                return root.epgEventJSONObject.rtn.Error;
            }
            return null;
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
