using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AmsMigrator.DTO.AMS1
{
    public partial class States
    {
        [JsonProperty("ru")]
        public Dictionary<string, string> Ru { get; set; }
    }

    public partial class States
    {
        public static States FromJson(string json) => JsonConvert.DeserializeObject<States>(json, Converter.Settings);
    }
}
