using Newtonsoft.Json.Linq;

namespace CloningTool.Json
{
    public class RemarkCategory
    {
        public long Id { get; set; }
        public JObject Name { get; set; }

        public override string ToString() => $"{Id} - {Name}";
    }
}
