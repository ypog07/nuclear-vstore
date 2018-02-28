using Newtonsoft.Json.Linq;

using NuClear.VStore.Descriptors;
using NuClear.VStore.Descriptors.Templates;
using NuClear.VStore.Json;

namespace NuClear.VStore.Sessions.ContentValidation.Errors
{
    public class ImageUnsupportedAspectRatioError : BinaryValidationError
    {
        public ImageAspectRatio ActualAspectRatio { get; }

        public ImageUnsupportedAspectRatioError(ImageAspectRatio actualAspectRatio)
        {
            ActualAspectRatio = actualAspectRatio.GetNormalized();
        }

        public override string ErrorType => nameof(ScalableBitmapImageElementConstraints.ImageAspectRatio);

        public override JToken SerializeToJson()
        {
            var ret = base.SerializeToJson();
            ret[Tokens.ValueToken] = JToken.FromObject(ActualAspectRatio, JsonSerializer);
            return ret;
        }
    }
}