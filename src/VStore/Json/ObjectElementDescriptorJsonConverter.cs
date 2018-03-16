using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NuClear.VStore.Descriptors.Objects;
using NuClear.VStore.Descriptors.Templates;

namespace NuClear.VStore.Json
{
    public sealed class ObjectElementDescriptorJsonConverter : JsonConverter<ObjectElementDescriptor>
    {
        public override ObjectElementDescriptor ReadJson(JsonReader reader, Type objectType, ObjectElementDescriptor existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var json = JObject.Load(reader);

            var valueToken = json[Tokens.ValueToken];
            var elementDescriptor = json.ToObject<IElementDescriptor>(serializer);

            var id = json[Tokens.IdToken].ToObject<long>();

            var versionId = string.Empty;
            var versionIdToken = json.SelectToken(Tokens.VersionIdToken);
            if (versionIdToken != null)
            {
                versionId = versionIdToken.ToObject<string>();
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

        public override void WriteJson(JsonWriter writer, ObjectElementDescriptor value, JsonSerializer serializer)
        {
            serializer.Converters.Remove(this);

            value.NormalizeValue();
            serializer.Serialize(writer, value);
        }
    }
}