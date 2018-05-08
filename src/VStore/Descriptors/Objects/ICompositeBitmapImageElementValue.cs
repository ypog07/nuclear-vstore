using System.Collections.Generic;

namespace NuClear.VStore.Descriptors.Objects
{
    public interface ICompositeBitmapImageElementValue : IImageElementValue
    {
        CropArea CropArea { get; set; }
        IEnumerable<SizeSpecificImage> SizeSpecificImages { get; set; }
    }
}