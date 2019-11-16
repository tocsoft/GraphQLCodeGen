using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Tocsoft.GraphQLCodeGen
{
    internal class SchemaSourceJsonConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return typeof(SchemaSource) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            SchemaSource result = (existingValue as SchemaSource ?? new SchemaSource());

            if (token is Newtonsoft.Json.Linq.JObject obj)
            {
                SchemaSource temp = token.ToObject<SchemaSource>();

                result.Location = temp.Location;
                result.MutationType = temp.MutationType;
                result.QueryType = temp.QueryType;
                result.Headers = temp.Headers;
            }
            else if (token is Newtonsoft.Json.Linq.JValue value)
            {
                result.Location = value.Value.ToString();
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
