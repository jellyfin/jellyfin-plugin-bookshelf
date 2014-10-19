using System;
using System.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.NextPvr.Helpers;

namespace MediaBrowser.Plugins.NextPvr.Responses
{
    public class InstantiateResponse
    {
        public ClientKeys GetClientKeys(Stream stream, IJsonSerializer json,ILogger logger)
        {
            var root = json.DeserializeFromStream<RootObject>(stream);

            if (root.clientKeys != null && root.clientKeys.sid != null && root.clientKeys.salt != null)
            {
                UtilsHelper.DebugInformation(logger,string.Format("[NextPvr] ClientKeys: {0}", json.SerializeToString(root)));
                return root.clientKeys;
            }
            logger.Error("[NextPvr] Failed to load the ClientKeys from NextPvr.");
            throw new ApplicationException("Failed to load the ClientKeys from NextPvr.");
        }

        public class ClientKeys
        {
            public string sid { get; set; }
            public string salt { get; set; }
        }

        private class RootObject
        {
            public ClientKeys clientKeys { get; set; }
        }
    }
}
