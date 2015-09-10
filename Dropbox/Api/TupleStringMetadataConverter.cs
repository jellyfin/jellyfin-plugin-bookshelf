using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dropbox.Api
{
    public class TupleStringMetadataConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JArray jarray = JArray.Load(reader);
            var item1 = jarray[0].ToString();
            var item2 = jarray[1].ToObject<MetadataResult>();

            return new List<Tuple<string, MetadataResult>>
            {
                new Tuple<string, MetadataResult>(item1, item2)
            };
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(List<Tuple<string, MetadataResult>>).IsAssignableFrom(objectType);
        }
    }
}
