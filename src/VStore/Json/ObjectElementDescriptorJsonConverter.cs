using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NuClear.VStore.Descriptors.Objects;
using NuClear.VStore.Descriptors.Templates;

namespace NuClear.VStore.Json
{
    public sealed class ObjectElementDescriptorJsonConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => typeof(ObjectElementDescriptor) == objectType;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject json;
            try
            {
                json = JObject.Load(reader);
            }
            catch (JsonReaderException ex)
            {
                throw new JsonSerializationException("Object element descriptor is not a valid JSON", ex);
            }

            var idToken = json[Tokens.IdToken];
            if (idToken == null)
            {
                throw new JsonSerializationException($"Some element has no '{Tokens.IdToken}' property.");
            }

            var versionId = string.Empty;
            var versionIdToken = json.SelectToken(Tokens.VersionIdToken);
            if (versionIdToken != null)
            {
                versionId = versionIdToken.ToObject<string>();
            }

            var id = idToken.ToObject<long>();
            var valueToken = json[Tokens.ValueToken];
            if (valueToken == null)
            {
                throw new JsonSerializationException($"Element with id '{id}' has no '{Tokens.ValueToken}' property.");
            }

            var elementDescriptor = json.ToObject<IElementDescriptor>(serializer);
            if (elementDescriptor == null)
            {
                throw new JsonSerializationException($"Element with id '{id}' has incorrect descriptor.");
            }

            var value = valueToken.AsObjectElementValue(elementDescriptor.Type);
            return new ObjectElementDescriptor
                {
                    Id = id,
                    VersionId = versionId,
                    Type = elementDescriptor.Type,
                    TemplateCode = elementDescriptor.TemplateCode,
                    Properties = elementDescriptor.Properties,
                    Constraints = elementDescriptor.Constraints,
                    Value = value
                };
        }
    }
}