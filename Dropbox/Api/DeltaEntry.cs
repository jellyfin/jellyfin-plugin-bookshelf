using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dropbox.Api
{
    public struct DeltaEntry
    {
        public string Path { get; set; }
        public MetadataResult Metadata { get; set; }

        // TODO: find how to use ServiceStack to deserialize this 2-items array of different types (Json.NET can be removed after that)
        public static DeltaEntry Parse(string json)
        {
            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new TupleStringMetadataConverter());
                return settings;
            };

            var tuple = JsonConvert.DeserializeObject<List<Tuple<string, MetadataResult>>>(json);

            return new DeltaEntry
            {
                Path = tuple[0].Item1,
                Metadata = tuple[0].Item2
            };
        }
    }
}
