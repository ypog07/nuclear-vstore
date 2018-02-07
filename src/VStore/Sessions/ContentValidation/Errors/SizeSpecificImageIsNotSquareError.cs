using NuClear.VStore.Descriptors.Templates;

namespace NuClear.VStore.Sessions.ContentValidation.Errors
{
    public class SizeSpecificImageIsNotSquareError : BinaryValidationError
    {
        public override string ErrorType => nameof(CompositeBitmapImageElementConstraints.SizeSpecificImageIsSquare);
    }
}