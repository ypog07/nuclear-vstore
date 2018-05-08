using System;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NuClear.VStore.Descriptors.Objects;
using NuClear.VStore.Descriptors.Objects.Persistence;
using NuClear.VStore.Descriptors.Templates;

namespace NuClear.VStore.Json
{
    public static class JTokenExtensions
    {
        private const Anchor DefaultAnchor = Anchor.Middle;

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
                    {
                        var value = valueToken.ToObject<ScalableBitmapImageElementValue>();
                        if (!Enum.IsDefined(typeof(Anchor), value.Anchor))
                        {
                            value.Anchor = DefaultAnchor;
                        }

                        return value;
                    }

                default:
                    throw new JsonSerializationException($"Unknown element type '{elementDescriptorType.ToString()}'");
            }
        }

        public static void NormalizeValue(this IObjectElementPersistenceDescriptor descriptor)
        {
            switch (descriptor.Value)
            {
                case CompositeBitmapImageElementValue compositeBitmapImageElementValue:
                    if (compositeBitmapImageElementValue.SizeSpecificImages == null)
                    {
                        compositeBitmapImageElementValue.SizeSpecificImages = Enumerable.Empty<SizeSpecificImage>();
                    }

                    break;
                case ScalableBitmapImageElementValue scalableBitmapImageElementValue:
                    if (!Enum.IsDefined(typeof(Anchor), scalableBitmapImageElementValue.Anchor))
                    {
                        scalableBitmapImageElementValue.Anchor = DefaultAnchor;
                    }

                    break;
            }
        }
    }
}