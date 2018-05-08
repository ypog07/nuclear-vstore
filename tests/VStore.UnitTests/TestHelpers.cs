using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Moq;

using NuClear.VStore.Descriptors;
using NuClear.VStore.Descriptors.Objects;
using NuClear.VStore.Descriptors.Templates;
using NuClear.VStore.Objects.ContentValidation.Errors;
using NuClear.VStore.Sessions;
using NuClear.VStore.Sessions.ContentValidation.Errors;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

using Xunit;

namespace VStore.UnitTests
{
    internal static class TestHelpers
    {
        internal delegate IEnumerable<ObjectElementValidationError> Validator(IObjectElementValue value, IElementConstraints elementConstraints);

        internal static TError MakeValidationCheck<TValue, TError>(TValue value, IElementConstraints constraints, Validator validator, Action<TValue> valueChanger)
            where TValue : IObjectElementValue
            where TError : ObjectElementValidationError
        {
            Assert.Empty(validator(value, constraints));
            valueChanger(value);

            var errors = validator(value, constraints).ToList();
            Assert.Single(errors);
            Assert.IsType<TError>(errors.First());

            return (TError)errors.First();
        }

        internal static TError MakeBinaryValidationCheck<TError>(string content, Action<int, Stream> testAction, string expectedErrorType, int templateCode = 1)
            where TError : BinaryValidationError
        {
            InvalidBinaryException ex;
            using (var stream = new MemoryStream())
            {
                using (var sw = new StreamWriter(stream, Encoding.ASCII))
                {
                    sw.Write(content);
                    sw.Flush();
                    stream.Position = 0;
                    ex = Assert.Throws<InvalidBinaryException>(() => testAction(templateCode, stream));
                }
            }

            Assert.IsType<TError>(ex.Error);
            Assert.Equal(templateCode, ex.TemplateCode);
            Assert.Equal(expectedErrorType, ex.Error.ErrorType);
            return (TError)ex.Error;
        }

        public static void MakeBinaryValidationCheck(string content, Action<int, Stream> testAction, int templateCode = 1)
        {
            using (var stream = new MemoryStream())
            {
                using (var sw = new StreamWriter(stream, Encoding.ASCII))
                {
                    sw.Write(content);
                    sw.Flush();
                    stream.Position = 0;
                    testAction(templateCode, stream);
                }
            }
        }

        internal static void InternalTextChecksTest(
            IEnumerable<Validator> allChecks,
            bool containsRestrictedSymbols,
            int expectedErrorsCount,
            IObjectElementValue value,
            TextElementConstraints constraints)
        {
            var errors = new List<ObjectElementValidationError>();
            foreach (var validator in allChecks)
            {
                errors.AddRange(validator(value, constraints));
            }

            Assert.Equal(expectedErrorsCount, errors.Count);
            if (containsRestrictedSymbols)
            {
                Assert.Single(errors.OfType<NonBreakingSpaceSymbolError>());
                Assert.Single(errors.OfType<ControlCharactersInTextError>());
            }

            if (constraints.MaxSymbols.HasValue)
            {
                Assert.Single(errors.OfType<ElementTextTooLongError>());
            }

            if (constraints.MaxLines.HasValue)
            {
                Assert.Single(errors.OfType<TooManyLinesError>());
            }

            if (constraints.MaxSymbolsPerWord.HasValue)
            {
                Assert.Single(errors.OfType<ElementWordsTooLongError>());
            }
        }

        internal static bool TestRouteConstraint(IRouteConstraint constraint, object value)
        {
            var route = new RouteCollection();
            var context = new Mock<HttpContext>();

            const string ParameterName = "fake";
            var values = new RouteValueDictionary { { ParameterName, value } };
            return constraint.Match(context.Object, route, ParameterName, values, RouteDirection.IncomingRequest);
        }

        internal static Stream CreateImage(int width, int height, IImageEncoder encoder)
        {
            var image = Image.LoadPixelData(new Rgba32[width * height], width, height);
            var ms = new MemoryStream();
            image.Save(ms, encoder);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        internal static IImageEncoder GetImageEncoder(FileFormat format)
        {
            switch (format)
            {
                case FileFormat.Png:
                    return new PngEncoder();
                case FileFormat.Gif:
                    return new GifEncoder();
                case FileFormat.Bmp:
                    return new BmpEncoder();
                case FileFormat.Jpg:
                case FileFormat.Jpeg:
                    return new JpegEncoder();
                case FileFormat.Chm:
                case FileFormat.Svg:
                case FileFormat.Pdf:
                    throw new InvalidOperationException("Incompatible image format: " + format.ToString());
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }
    }
}
