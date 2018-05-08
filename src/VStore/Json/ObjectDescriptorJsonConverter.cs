using System;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NuClear.VStore.Descriptors.Objects;

namespace NuClear.VStore.Json
{
    public sealed class ObjectDescriptorJsonConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => typeof(IObjectDescriptor).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj;
            try
            {
                obj = JObject.Load(reader);
            }
            catch (JsonReaderException ex)
            {
                throw new JsonSerializationException("Object descriptor is not a valid JSON", ex);
            }

            var descriptors = obj[Tokens.ElementsToken];
            if (descriptors == null)
            {
                throw new JsonSerializationException($"Object descriptor doesn't contain '{Tokens.ElementsToken}' token");
            }

            var elementDescriptors = descriptors.Select(x => x.ToObject<ObjectElementDescriptor>(serializer))
                                                .ToList();

            obj.Remove(Tokens.ElementsToken);

            var objectDescriptor = obj.ToObject<ObjectDescriptor>();
            objectDescriptor.Elements = elementDescriptors;

            return objectDescriptor;
        }
    }
}
