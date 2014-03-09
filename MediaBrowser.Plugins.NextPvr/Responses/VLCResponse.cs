using System;
using System.IO;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.NextPvr.Responses
{
    public class VLCResponse    
    {
        public VLCObj GetVLCResponse(Stream stream, IJsonSerializer json)
        {
            var root = json.DeserializeFromStream<RootObject>(stream);

            if (root.JSONVlcObject.VLC_Obj != null && root.JSONVlcObject.VLC_Obj.isVlcAvailable == true)
            {
                return root.JSONVlcObject.VLC_Obj;
            }
            throw new ApplicationException("Failed to load the VLC from NEWA.");
        }
        public Rtn GetVLCReturn(Stream stream, IJsonSerializer json)
        {
            var root = json.DeserializeFromStream<RootObject>(stream);
            return root.JSONVlcObject.rtn;
        }
        public class VLCObj
        {
            public bool isVlcAvailable { get; set; }
            public string StreamLocation { get; set; }
            public int ProcessId { get; set; }
        }

        public class Rtn
        {
            public bool Error { get; set; }
            public int HTTPStatus { get; set; }
            public string Message { get; set; }
        }

        private class JSONVlcObject
        {
            public VLCObj VLC_Obj { get; set; }
            public Rtn rtn { get; set; }
        }

        private class RootObject
        {
            public JSONVlcObject JSONVlcObject { get; set; }
        }
    }
}
