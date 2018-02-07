using Newtonsoft.Json;

namespace AmsMigrator.DTO.AMS1
{

    public partial class Amsv1Response
    {
        public MaterialMetadata Meta { get; set; }
        public Result Result { get; set; }
    }

    public class Result
    {
        public Item[] Items { get; set; }

        [JsonProperty("total_count")]
        public long TotalCount { get; set; }
    }

    public class Item
    {
        public string Author { get; set; }

        public string Commit { get; set; }

        [JsonProperty("communication_languages")]
        public string[] CommunicationLanguages { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("firm_id")]
        public string FirmId { get; set; }

        public string Lang { get; set; }

        [JsonProperty("region_id")]
        public string RegionId { get; set; }

        public string State { get; set; }

        public string Template { get; set; }

        public string Uuid { get; set; }
    }

    public class MaterialMetadata
    {
        public long Code { get; set; }

        public string Text { get; set; }
    }

    public partial class Amsv1Response
    {
        public static Amsv1Response FromJson(string json) => JsonConvert.DeserializeObject<Amsv1Response>(json, Converter.Settings);
    }
}
