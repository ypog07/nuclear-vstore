using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NuClear.VStore.Json;

namespace CloningTool.Json
{
    public sealed class ApiTemplateDescriptorJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(ApiTemplateDescriptor).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var templateDescriptor = (ApiTemplateDescriptor)value;
            var json = new JObject
                {
                    [Tokens.PropertiesToken] = templateDescriptor.Properties,
                    [Tokens.ElementsToken] = JArray.FromObject(templateDescriptor.Elements, serializer)
                };
            json.WriteTo(writer);
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
                throw new JsonSerializationException("Template descriptor is not a valid JSON", ex);
            }

            var descriptors = obj[Tokens.ElementsToken];
            if (descriptors == null)
            {
                throw new JsonSerializationException($"Template descriptor doesn't contain '{Tokens.ElementsToken}' token");
            }

            var elementDescriptors = descriptors.ToObject<IReadOnlyCollection<ApiTemplateElementDescriptor>>(serializer);

            obj.Remove(Tokens.ElementsToken);
            var templateDescriptor = obj.ToObject<ApiTemplateDescriptor>();
            templateDescriptor.Elements = elementDescriptors;

            return templateDescriptor;
        }
    }
}