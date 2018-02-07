using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace AmsMigrator.DTO.Okapi
{
    public partial class MaterialStub
    {
        public Element[] Elements { get; set; }

        public long Id { get; set; }
        public string Language { get; set; }
        public Properties Properties { get; set; }
        public string TemplateId { get; set; }
        public string TemplateVersionId { get; set; }
        public string VersionId { get; set; }
    }

    public class Properties
    {
        public bool IsNew { get; set; }
        public string Name { get; set; }
        public TemplateName TemplateName { get; set; }
    }

    public class Element
    {
        public JObject Constraints { get; set; }
        public string Id { get; set; }
        public JObject Properties { get; set; }
        public string TemplateCode { get; set; }
        public string Type { get; set; }
        public string UploadUrl { get; set; }
        public Value Value { get; set; }
    }

    public class Value
    {
        public string Raw { get; set; }
        public CropArea CropArea { get; set; }
        public SizeSpecificImage[] SizeSpecificImages { get; set; }
    }

    public class TemplateName
    {
        public string Ru { get; set; }
    }

    public partial class MaterialStub
    {
        public static MaterialStub[] FromJson(string json) => JsonConvert.DeserializeObject<MaterialStub[]>(json, Converter.Settings);
        public static MaterialStub SingleFromJson(string json) => JsonConvert.DeserializeObject<MaterialStub>(json, Converter.Settings);
    }
}
