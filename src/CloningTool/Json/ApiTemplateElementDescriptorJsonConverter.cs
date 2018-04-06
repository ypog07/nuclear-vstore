using System;

using Newtonsoft.Json;

using NuClear.VStore.Json;

namespace CloningTool.Json
{
    public class ApiTemplateElementDescriptorJsonConverter : JsonConverter<ITemplateElementDescriptor>
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, ITemplateElementDescriptor value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override ITemplateElementDescriptor ReadJson(JsonReader reader, Type objectType, ITemplateElementDescriptor existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var innerElementConverter = new ElementDescriptorJsonConverter();
            var innerValue = innerElementConverter.ReadJson(reader, objectType, existingValue, hasExistingValue, serializer);

            var placementToken = innerElementConverter.LoadedObject[Tokens.PlacementToken];
            if (placementToken == null)
            {
                throw new JsonSerializationException($"Template element with template code '{innerValue.TemplateCode}' doesn't contain '{Tokens.PlacementToken}' property.");
            }

            var placement = placementToken.ToObject<PlacementDescriptor>();
            if (placement == null)
            {
                throw new JsonSerializationException($"Template element with template code '{innerValue.TemplateCode}' has empty '{Tokens.PlacementToken}' property.");
            }

            return new ApiTemplateElementDescriptor(innerValue.Type, innerValue.TemplateCode, innerValue.Properties, innerValue.Constraints, placement);
        }
    }
}