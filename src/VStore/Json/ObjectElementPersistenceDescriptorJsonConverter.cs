using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NuClear.VStore.Descriptors.Objects.Persistence;
using NuClear.VStore.Descriptors.Templates;

namespace NuClear.VStore.Json
{
    public sealed class ObjectElementPersistenceDescriptorJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(IObjectElementPersistenceDescriptor).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Converters.Remove(this);

            var persistenceDescriptor = (IObjectElementPersistenceDescriptor)value;
            persistenceDescriptor.NormalizeValue();
            serializer.Serialize(writer, persistenceDescriptor);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var json = JObject.Load(reader);

            var elementDescriptor = json.ToObject<IElementDescriptor>(serializer);

            var valueToken = json[Tokens.ValueToken];
            if (valueToken == null)
            {
                throw new JsonSerializationException($"Element with templateCode {elementDescriptor.TemplateCode} has no '{Tokens.ValueToken}' property.");
            }

            var value = valueToken.AsObjectElementValue(elementDescriptor.Type);

            return new ObjectElementPersistenceDescriptor(elementDescriptor, value);
        }
    }
}