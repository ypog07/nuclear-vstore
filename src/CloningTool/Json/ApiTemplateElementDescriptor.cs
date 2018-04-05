using NuClear.VStore.Descriptors.Templates;

using Newtonsoft.Json.Linq;

namespace CloningTool.Json
{
    public sealed class ApiTemplateElementDescriptor : ITemplateElementDescriptor
    {
        public ApiTemplateElementDescriptor(ElementDescriptorType descriptorType, int templateCode, JObject properties, ConstraintSet constraintSet, PlacementDescriptor placement)
        {
            Type = descriptorType;
            TemplateCode = templateCode;
            Properties = properties;
            Constraints = constraintSet;
            Placement = placement;
        }

        public ElementDescriptorType Type { get; set; }
        public int TemplateCode { get; set; }
        public JObject Properties { get; set; }
        public ConstraintSet Constraints { get; set; }
        public PlacementDescriptor Placement { get; set; }
    }
}
