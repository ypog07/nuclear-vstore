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
                    { ImageFormats.Gif.DefaultMimeType, new PngEncoder { CompressionLevel = 1, IgnoreMetadata = true } },
                    { ImageFormats.Bmp.DefaultMimeType, new PngEncoder { CompressionLevel = 1, IgnoreMetadata = true } }
                };

        private static readonly Dictionary<string, IImageFormat> Formats =
            new Dictionary<string, IImageFormat>
                {
                    { ImageFormats.Png.DefaultMimeType, ImageFormats.Png },
                    { ImageFormats.Gif.DefaultMimeType, ImageFormats.Png },
                    { ImageFormats.Bmp.DefaultMimeType, ImageFormats.Png }
                };

        public static (byte[] Data, string Ext) FillImageBackground(byte[] input, string color)
        {
            try
            {
                using (Image<Rgba32> image = Image.Load(input, out var fmt))
                using (var outputStream = new MemoryStream())
                {
                    if (Encoders.TryGetValue(fmt.DefaultMimeType, out var encoder))
                    {
                        var isAlpha = IsImageContainsAlphaChannel(image);

                        if (!string.IsNullOrEmpty(color) && isAlpha)
                        {
                            image.Mutate(x => x.BackgroundColor(Rgba32.FromHex(color)));
                        }

                        var replacedFormat = Formats[fmt.DefaultMimeType];

                        if (fmt.DefaultMimeType.Equals(replacedFormat.DefaultMimeType) && !isAlpha)
                        {
                            return (input, fmt.FileExtensions.First());
                        }

                        image.Save(outputStream, encoder);

                        return (outputStream.ToArray(), replacedFormat.FileExtensions.First());
                    }

                    return (input, fmt.FileExtensions.First());
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Exception occured in ImageSharp while filling image background. Original will be imported instead.");
                return (input, null);
            }
        }

        private static bool IsImageContainsAlphaChannel(Image<Rgba32> image)
        {
            for (var x = 0; x < image.Width; ++x)
            {
                for (var y = 0; y < image.Height; ++y)
                {
                    if (image[x, y].A != byte.MaxValue)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
