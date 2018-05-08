using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.VStore.Descriptors.Objects;
using NuClear.VStore.Descriptors.Templates;
using NuClear.VStore.Objects.ContentValidation.Errors;

namespace NuClear.VStore.Objects.ContentValidation
{
    public static class CompositeBitmapImageValidator
    {
        public static IEnumerable<ObjectElementValidationError> CheckValidCompositeBitmapImage(IObjectElementValue value, IElementConstraints constraints)
        {
            var compositeBitmapImage = (ICompositeBitmapImageElementValue)value;
            if (string.IsNullOrEmpty(compositeBitmapImage.Raw))
            {
                return Enumerable.Empty<ObjectElementValidationError>();
            }

            var compositeBitmapImageElementConstraints = (CompositeBitmapImageElementConstraints)constraints;
            var cropAreaIsSquare = compositeBitmapImage.CropArea != null && compositeBitmapImage.CropArea.Width == compositeBitmapImage.CropArea.Height;
            if (compositeBitmapImageElementConstraints.CropAreaIsSquare && !cropAreaIsSquare)
            {
                return new[] { new CropAreaIsNotSquareError() };
            }

            return Array.Empty<ObjectElementValidationError>();
        }
    }
}