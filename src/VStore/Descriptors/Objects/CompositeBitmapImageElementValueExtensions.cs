using System.Linq;

namespace NuClear.VStore.Descriptors.Objects
{
    public static class CompositeBitmapImageElementValueExtensions
    {
        public static bool TryGetSizeSpecificBitmapImageRawValue(this IImageElementValue imageElementValue, int width, int height, out string rawValue)
        {
            rawValue = null;
            if (imageElementValue is ICompositeBitmapImageElementValue compositeBitmapImageElementValue)
            {
                var sizeSpecificImage = compositeBitmapImageElementValue.SizeSpecificImages
                                                                        .SingleOrDefault(x => x.Size.Width == width && x.Size.Height == height);
                if (sizeSpecificImage != null)
                {
                    rawValue = sizeSpecificImage.Raw;
                    return true;
                }

                return false;
            }

            return false;
        }
    }
}