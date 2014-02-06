using System;
using System.IO;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.NextPvr.Responses
{
    public class InitializeResponse
    {
        public bool LoggedIn(Stream stream, IJsonSerializer json)
        {
            var root = json.DeserializeFromStream<RootObject>(stream);

            if (root.SIDValidation != null)
            {
                return root.SIDValidation.validated;
            }
            throw new ApplicationException("Failed to validate your connection with NextPvr.");
        }

        public class SIDValidation
        {
            public bool validated { get; set; }
        }

        public class RootObject
        {
            public SIDValidation SIDValidation { get; set; }
        }
    }
}
