using NuClear.VStore.Descriptors.Templates;

namespace CloningTool.Json
{
    public interface ITemplateElementDescriptor : IElementDescriptor
    {
        PlacementDescriptor Placement { get; set; }
    }
}