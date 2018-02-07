using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AmsMigrator.DTO.AMS1
{
    public partial class LogoInfo
    {
        public Commit Commit { get; set; }
        public Datum[] Data { get; set; }
        public Meta Meta { get; set; }
    }

    public class Meta
    {
        [JsonProperty("communication_languages")]
        public string[] CommunicationLanguages { get; set; }

        [JsonProperty("firm_id")]
        public string FirmId { get; set; }

        [JsonProperty("region_id")]
        public string RegionId { get; set; }

        public string Template { get; set; }
    }

    public class Datum
    {
        public dynamic Content { get; set; }

        public string Ext { get; set; }

        public int? Height { get; set; }

        public int? Width { get; set; }
        
        public string Name { get; set; }

        public long? Size { get; set; }

        public string Type { get; set; }

        public string Url { get; set; }
    }

    public partial class PurpleContent
    {
        public long Height { get; set; }

        public long Left { get; set; }

        public long Top { get; set; }

        public long Width { get; set; }
    }

    public class Commit
    {
        public string Author { get; set; }

        public string Hash { get; set; }

        public string Lang { get; set; }

        public string State { get; set; }
        public string Timestamp { get; set; }
        public Moderation Moderation { get; set; }
    }

    public class Moderation
    {
        public string Overall { get; set; }
    }

    public partial class LogoInfo
    {
        public static LogoInfo FromJson(string json) => JsonConvert.DeserializeObject<LogoInfo>(json, Converter.Settings);
    }
}
