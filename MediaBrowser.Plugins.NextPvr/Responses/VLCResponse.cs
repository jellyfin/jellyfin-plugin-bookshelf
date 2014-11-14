using System;
using System.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.NextPvr.Helpers;

namespace MediaBrowser.Plugins.NextPvr.Responses
{
    public class VLCResponse    
    {
        public VLCObj GetVLCResponse(Stream stream, IJsonSerializer json, ILogger logger)
        {
            var root = json.DeserializeFromStream<RootObject>(stream);

            if (root.JSONVlcObject.VLC_Obj != null && root.JSONVlcObject.VLC_Obj.isVlcAvailable == true)
            {
                UtilsHelper.DebugInformation(logger,string.Format("[NextPvr] VLC Response: {0}", json.SerializeToString(root)));
                return root.JSONVlcObject.VLC_Obj;
            }
            logger.Error("[NextPvr] Failed to load the VLC from NEWA");
            throw new ApplicationException("Failed to load the VLC from NEWA.");
        }
        public Rtn GetVLCReturn(Stream stream, IJsonSerializer json, ILogger logger)
        {
            var root = json.DeserializeFromStream<RootObject>(stream);
            UtilsHelper.DebugInformation(logger,string.Format("[NextPvr] VLC Return: {0}", json.SerializeToString(root)));
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
