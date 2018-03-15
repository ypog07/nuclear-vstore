using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using NuClear.VStore.Descriptors.Objects;

namespace NuClear.VStore.Json
{
    public sealed class AnchorJsonConverter : JsonConverter
    {
        private const Anchor DefaultValue = Anchor.Middle;

        private readonly JsonConverter _innerConverter;

        public AnchorJsonConverter()
        {
            _innerConverter = new StringEnumConverter(true);
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(Anchor);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var anchor = (Anchor)value;
            _innerConverter.WriteJson(writer, (int)anchor == 0 ? DefaultValue : anchor, serializer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var anchor = (Anchor)existingValue;
            return _innerConverter.ReadJson(reader, objectType, (int)anchor == 0 ? DefaultValue : anchor, serializer);
        }
    }
}