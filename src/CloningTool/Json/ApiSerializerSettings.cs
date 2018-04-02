using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using NuClear.VStore.Json;

namespace CloningTool.Json
{
    internal static class ApiSerializerSettings
    {
        private static readonly JsonConverter[] CustonConverters =
            {
                new StringEnumConverter { CamelCaseText = true },
                new ElementDescriptorJsonConverter(),
                new ElementDescriptorCollectionJsonConverter(),
                new ApiObjectElementDescriptorJsonConverter(),
                new ApiTemplateDescriptorJsonConverter(),
                new ApiTemplateElementDescriptorJsonConverter()
            };

        static ApiSerializerSettings()
        {
            Default = new JsonSerializerSettings
                {
                    Culture = CultureInfo.InvariantCulture,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
            for (var index = 0; index < CustonConverters.Length; index++)
            {
                Default.Converters.Insert(index, CustonConverters[index]);
            }
        }

        public static JsonSerializerSettings Default { get; }
    }
}
