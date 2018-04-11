using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NuClear.VStore.Descriptors;
using NuClear.VStore.Descriptors.Templates;
using NuClear.VStore.Json;

using Xunit;

namespace VStore.UnitTests.Json
{
    public sealed class TemplateDeserializationTests
    {
        [Fact]
        public void ShouldThrowOnInvalidJson()
        {
            const string JsonString = @"{ ; }";
            Assert.ThrowsAny<JsonException>(() => JsonConvert.DeserializeObject<ITemplateDescriptor>(JsonString, SerializerSettings.Default));
        }

        [Fact]
        public void ShouldThrowWithoutElements()
        {
            const string JsonString = @"{ }";
            Assert.ThrowsAny<JsonException>(() => JsonConvert.DeserializeObject<ITemplateDescriptor>(JsonString, SerializerSettings.Default));
        }

        [Fact]
        public void ShouldDeserializeEmptyDescriptor()
        {
            const string JsonString = @"{ ""elements"": [] }";
            var templateDescriptor = JsonConvert.DeserializeObject<ITemplateDescriptor>(JsonString, SerializerSettings.Default);
            Assert.NotNull(templateDescriptor);
            Assert.Empty(templateDescriptor.Elements);
        }

        [Fact]
        public void ShouldDeserializeValidDescriptor()
        {
            const string JsonString =
@"{
    ""id"": 100500,
    ""versionId"": ""j;lkj:LK;jhHlkjhlI*Hljhlihl"",
    ""properties"": {
        ""foo"": ""bar"",
        ""baz"": 123
    },
    ""elements"": [{
        ""type"": ""scalableBitmapImage"",
        ""templateCode"": 1,
        ""properties"": {
            ""baz"": [ 321, 456 ],
            ""foo"": ""bar""
        },
        ""constraints"": {
            ""ru"": {
                ""supportedFileFormats"": [""png"", ""gif""],
                ""imageSizeRange"": {
                    ""min"": {
                        ""width"": 1,
                        ""height"": 2
                    },
                    ""max"": {
                        ""width"": 10,
                        ""height"": 11
                    }
                }
            }
        }
    }]
}";

            var templateDescriptor = JsonConvert.DeserializeObject<ITemplateDescriptor>(JsonString, SerializerSettings.Default);
            Assert.NotNull(templateDescriptor);
            Assert.Equal(JObject.Parse(@"{""foo"": ""bar"", ""baz"": 123}"), templateDescriptor.Properties, new JTokenEqualityComparer());
            Assert.Single(templateDescriptor.Elements);

            var element = templateDescriptor.Elements.First();
            Assert.IsType<ElementDescriptor>(element);
            Assert.Equal(ElementDescriptorType.ScalableBitmapImage, element.Type);
            Assert.Equal(1, element.TemplateCode);
            Assert.Equal(JObject.Parse(@"{""foo"": ""bar"", ""baz"": [ 321, 456 ]}"), element.Properties, new JTokenEqualityComparer());
            Assert.Single(element.Constraints);

            var constraintSetItem = element.Constraints.First<ConstraintSetItem>();
            Assert.Equal(Language.Ru, constraintSetItem.Language);
            Assert.IsType<ScalableBitmapImageElementConstraints>(constraintSetItem.ElementConstraints);

            var constraints = (ScalableBitmapImageElementConstraints)constraintSetItem.ElementConstraints;
            Assert.Null(constraints.MaxSize);
            Assert.Null(constraints.MaxFilenameLength);
            Assert.Collection(constraints.SupportedFileFormats, x => Assert.Equal(FileFormat.Png, x), x => Assert.Equal(FileFormat.Gif, x));

            var expectedSizeRange = new ImageSizeRange
                {
                    Min = new ImageSize { Height = 2, Width = 1 },
                    Max = new ImageSize { Width = 10, Height = 11 }
                };

            Assert.Equal(expectedSizeRange, constraints.ImageSizeRange);
        }
    }
}
