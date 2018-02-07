using Newtonsoft.Json;

namespace AmsMigrator.DTO.Okapi
{
    public static class SerializeHepler
    {
        public static string ToJson(this MaterialStub[] self) => JsonConvert.SerializeObject(self, Converter.Settings);
        public static string ToJson(this MaterialStub self) => JsonConvert.SerializeObject(self, Converter.Settings);
        public static string ToJson(this ModerationRequest self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }
}
