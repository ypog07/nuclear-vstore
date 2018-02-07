using System.Collections.Generic;
using System.Linq;

namespace NuClear.VStore.Descriptors.Objects
{
    public static class ObjectElementValueExtensions
    {
        public static IEnumerable<string> ExtractFileKeys(this IObjectElementValue objectElementValue)
        {
            if (!(objectElementValue is IBinaryElementValue binaryElementValue) || string.IsNullOrEmpty(binaryElementValue.Raw))
            {
                return Enumerable.Empty<string>();
            }

            var binaryRawValues = new[] { binaryElementValue.Raw };
            return binaryElementValue is ICompositeBitmapImageElementValue compositeBitmapImageElementValue
                       ? binaryRawValues.Concat(compositeBitmapImageElementValue.SizeSpecificImages.Select(x => x.Raw))
                       : binaryRawValues;
        }
    }
}