using NuClear.VStore.Descriptors;
using NuClear.VStore.Descriptors.Templates;
using NuClear.VStore.Sessions;
using NuClear.VStore.Sessions.ContentValidation;
using NuClear.VStore.Sessions.ContentValidation.Errors;

using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;

using Xunit;

namespace VStore.UnitTests.Validation
{
    public class ScalableBitmapImageValidationTests
    {

        [Theory]
        [InlineData(FileFormat.Png)]
        [InlineData(FileFormat.Gif)]
        [InlineData(FileFormat.Jpeg)]
        [InlineData(FileFormat.Jpg)]
        public void ValidImage(FileFormat format)
        {
            var width = 5;
            var height = 5;

            var encoder = TestHelpers.GetImageEncoder(format);
            using (var image = TestHelpers.CreateImage(width, height, encoder))
            {
                var constraints = CreateConstraints(1, 1, 10, 10, format);
                BitmapImageValidator.ValidateScalableBitmapImageHeader(1, constraints, format, image);
            }
        }

        [Theory]
        [InlineData(FileFormat.Png, FileFormat.Jpeg)]
        [InlineData(FileFormat.Png, FileFormat.Jpg)]
        [InlineData(FileFormat.Png, FileFormat.Gif)]
        [InlineData(FileFormat.Gif, FileFormat.Png)]
        [InlineData(FileFormat.Gif, FileFormat.Jpeg)]
        [InlineData(FileFormat.Gif, FileFormat.Jpg)]
        [InlineData(FileFormat.Jpg, FileFormat.Gif)]
        [InlineData(FileFormat.Jpg, FileFormat.Png)]
        [InlineData(FileFormat.Jpeg, FileFormat.Gif)]
        [InlineData(FileFormat.Jpeg, FileFormat.Png)]
        public void InvalidImageFormat(FileFormat expectedFormat, FileFormat actualFormat)
        {
            var encoder = TestHelpers.GetImageEncoder(actualFormat);
            using (var image = TestHelpers.CreateImage(5, 5, encoder))
            {
                var constraints = CreateConstraints(1, 1, 10, 10, expectedFormat);

                var ex = Assert.Throws<InvalidBinaryException>(() => BitmapImageValidator.ValidateScalableBitmapImageHeader(1, constraints, actualFormat, image));
                Assert.IsType<BinaryInvalidFormatError>(ex.Error);
            }
        }

        [Theory]
        [InlineData(5, 5, 1, 1, 10, 10)]
        [InlineData(5, 5, 1, 1, 5, 5)]
        [InlineData(5, 5, 5, 5, 10, 10)]
        [InlineData(5, 5, 5, 5, 5, 5)]
        public void ValidImageSize(int width, int height, int minWidth, int minHeight, int maxWidth, int maxHeight)
        {
            const FileFormat PngFormat = FileFormat.Png;
            using (var image = TestHelpers.CreateImage(width, height, new PngEncoder()))
            {
                var constraints = CreateConstraints(minWidth, minHeight, maxWidth, maxHeight, PngFormat);
                BitmapImageValidator.ValidateScalableBitmapImageHeader(1, constraints, PngFormat, image);
            }
        }

        [Theory]
        [InlineData(1, 1, 5, 5, 10, 10)]
        [InlineData(1, 7, 5, 5, 10, 10)]
        [InlineData(7, 1, 5, 5, 10, 10)]
        [InlineData(7, 11, 5, 5, 10, 10)]
        public void InvalidImageSize(int width, int height, int minWidth, int minHeight, int maxWidth, int maxHeight)
        {
            const FileFormat GifFormat = FileFormat.Gif;
            using (var image = TestHelpers.CreateImage(width, height, new GifEncoder()))
            {
                var constraints = CreateConstraints(minWidth, minHeight, maxWidth, maxHeight, GifFormat);

                var ex = Assert.Throws<InvalidBinaryException>(() => BitmapImageValidator.ValidateScalableBitmapImageHeader(1, constraints, GifFormat, image));
                Assert.IsType<ImageSizeOutOfRangeError>(ex.Error);
            }
        }

        [Theory]
        [InlineData(5, 5, 1.0)]
        [InlineData(5, 10, 0.5)]
        [InlineData(10, 5, 2.0)]
        [InlineData(7, 3, 2.33)]
        public void ValidImageAspectRatio(int width, int height, decimal aspectRatio)
        {
            const FileFormat PngFormat = FileFormat.Png;
            using (var image = TestHelpers.CreateImage(width, height, new PngEncoder()))
            {
                var constraints = CreateConstraints(width, height, width, height, PngFormat, aspectRatio);
                BitmapImageValidator.ValidateScalableBitmapImageHeader(1, constraints, PngFormat, image);
            }
        }

        [Theory]
        [InlineData(5, 5, 1.1)]
        [InlineData(5, 10, -1.0)]
        [InlineData(10, 5, 1.9)]
        [InlineData(7, 3, 2.45)]
        public void InvalidImageAspectRatio(int width, int height, decimal aspectRatio)
        {
            const FileFormat GifFormat = FileFormat.Gif;
            using (var image = TestHelpers.CreateImage(width, height, new GifEncoder()))
            {
                var constraints = CreateConstraints(width, height, width, height, GifFormat, aspectRatio);

                var ex = Assert.Throws<InvalidBinaryException>(() => BitmapImageValidator.ValidateScalableBitmapImageHeader(1, constraints, GifFormat, image));
                Assert.IsType<ImageUnsupportedAspectRatioError>(ex.Error);
            }
        }

        private static ScalableBitmapImageElementConstraints CreateConstraints(int minWidth,
                                                                               int minHeight,
                                                                               int maxWidth,
                                                                               int maxHeight,
                                                                               FileFormat format,
                                                                               decimal? aspectRatio = null) =>
            new ScalableBitmapImageElementConstraints
                {
                    ImageSizeRange = new ImageSizeRange
                        {
                            Min = new ImageSize { Height = minHeight, Width = minWidth },
                            Max = new ImageSize { Height = maxHeight, Width = maxWidth }
                        },
                    SupportedFileFormats = new[] { format },
                    ImageAspectRatio = aspectRatio
                };
    }
}