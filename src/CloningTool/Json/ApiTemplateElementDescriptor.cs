using NuClear.VStore.Descriptors.Templates;

using Newtonsoft.Json.Linq;

namespace CloningTool.Json
{
    public sealed class ApiTemplateElementDescriptor
    {
        public ElementDescriptorType Type { get; set; }
        public int TemplateCode { get; set; }
        public JObject Properties { get; set; }
        public ConstraintSet Constraints { get; set; }
        public PlacementDescriptor Placement { get; set; }
    }
}
