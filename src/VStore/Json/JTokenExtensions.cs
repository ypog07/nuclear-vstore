using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NuClear.VStore.Descriptors.Objects;
using NuClear.VStore.Descriptors.Templates;

namespace NuClear.VStore.Json
{
    public static class JTokenExtensions
    {
        public static IObjectElementValue AsObjectElementValue(this JToken valueToken, ElementDescriptorType elementDescriptorType)
        {
            switch (elementDescriptorType)
            {
                case ElementDescriptorType.PlainText:
                case ElementDescriptorType.FormattedText:
                case ElementDescriptorType.Link:
                case ElementDescriptorType.VideoLink:
                    return valueToken.ToObject<TextElementValue>();
                case ElementDescriptorType.BitmapImage:
                    return valueToken.ToObject<BitmapImageElementValue>();
                case ElementDescriptorType.VectorImage:
                    return valueToken.ToObject<VectorImageElementValue>();
                case ElementDescriptorType.Article:
                    return valueToken.ToObject<ArticleElementValue>();
                case ElementDescriptorType.FasComment:
                    return valueToken.ToObject<FasElementValue>();
                case ElementDescriptorType.Phone:
                    return valueToken.ToObject<PhoneElementValue>();
                case ElementDescriptorType.Color:
                    return valueToken.ToObject<ColorElementValue>();
                case ElementDescriptorType.CompositeBitmapImage:
                {
                    var value = valueToken.ToObject<CompositeBitmapImageElementValue>();
                    if (value.SizeSpecificImages == null)
                    {
                        value.SizeSpecificImages = Enumerable.Empty<SizeSpecificImage>();
                    }

                    return value;
                }
                case ElementDescriptorType.ScalableBitmapImage:
                    return valueToken.ToObject<ScalableBitmapImageElementValue>();
                default:
                    throw new JsonSerializationException($"Unknown element type '{elementDescriptorType.ToString()}'");
            }
        }
    }
}