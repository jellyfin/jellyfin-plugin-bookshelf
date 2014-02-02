using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.NextPvr
{
    public class InstantiateResponse
    {
        public static ClientKeys GetClientKeys(Stream stream, IJsonSerializer json)
        {
            var root = json.DeserializeFromStream<RootObject>(stream);

            if (root.clientKeys != null && root.clientKeys.sid != null && root.clientKeys.salt != null)
            {
                return root.clientKeys;
            }
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
