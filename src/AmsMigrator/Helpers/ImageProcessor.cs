using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AmsMigrator.Helpers
{
    public class ImageProcessor
    {
        private static ILogger _logger = Log.Logger;

        private static readonly Dictionary<string, IImageEncoder> Encoders =
            new Dictionary<string, IImageEncoder>
                {
                    { ImageFormats.Png.DefaultMimeType, new PngEncoder { CompressionLevel = 1, IgnoreMetadata = true } },
                    { ImageFormats.Gif.DefaultMimeType, new PngEncoder { CompressionLevel = 1, IgnoreMetadata = true } }
                };

        private static readonly Dictionary<string, IImageFormat> Formats =
            new Dictionary<string, IImageFormat>
                {
                    { ImageFormats.Png.DefaultMimeType, ImageFormats.Png },
                    { ImageFormats.Gif.DefaultMimeType, ImageFormats.Png }
                };

        public static (byte[] Data, string Ext) FillImageBackground(byte[] input, string color)
        {
            try
            {
                using (Image<Rgba32> image = Image.Load(input, out var fmt))
                using (var outputStream = new MemoryStream())
                {
                    image.Mutate(x => x.BackgroundColor(Rgba32.FromHex(color)));

                    if (Encoders.TryGetValue(fmt.DefaultMimeType, out var encoder))
                    {
                        image.Save(outputStream, encoder);
                        var replacedFormat = Formats[fmt.DefaultMimeType];

                        return (outputStream.ToArray(), replacedFormat.FileExtensions.First());
                    }
                    else
                    {
                        return (input, fmt.FileExtensions.First());
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Exception occured in ImageSharp while filling image background. Original will be imported instead.");
                return (input, null);
            }
        }
    }
}
