using Newtonsoft.Json.Linq;

using NuClear.VStore.Descriptors.Templates;
using NuClear.VStore.Json;

namespace NuClear.VStore.Sessions.ContentValidation.Errors
{
    public class ImageUnsupportedAspectRatioError : BinaryValidationError
    {
        public decimal ActualAspectRatio { get; }

        public ImageUnsupportedAspectRatioError(decimal actualAspectRatio)
        {
            ActualAspectRatio = actualAspectRatio;
        }

        public override string ErrorType => nameof(ScalableBitmapImageElementConstraints.ImageAspectRatio);

        public override JToken SerializeToJson()
        {
            var ret = base.SerializeToJson();
            ret[Tokens.ValueToken] = ActualAspectRatio;
            return ret;
        }
    }
}