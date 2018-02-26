﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NuClear.VStore.Descriptors;
using NuClear.VStore.Descriptors.Templates;
using NuClear.VStore.Sessions.ContentValidation.Errors;

using SixLabors.ImageSharp;

namespace NuClear.VStore.Sessions.ContentValidation
{
    public static class BitmapImageValidator
    {
        private const decimal AspectRatioEpsilon = 0.1M;

        private static readonly IReadOnlyDictionary<FileFormat, string> ImageFormatToMimeTypeMap =
            new Dictionary<FileFormat, string>
                {
                    { FileFormat.Bmp, ImageFormats.Bmp.DefaultMimeType },
                    { FileFormat.Gif, ImageFormats.Gif.DefaultMimeType },
                    { FileFormat.Jpeg, ImageFormats.Jpeg.DefaultMimeType },
                    { FileFormat.Jpg, ImageFormats.Jpeg.DefaultMimeType },
                    { FileFormat.Png, ImageFormats.Png.DefaultMimeType }
                };

        public static void ValidateBitmapImageHeader(int templateCode, BitmapImageElementConstraints constraints, FileFormat fileFormat, Stream inputStream)
        {
            var imageInfo = ValidateBitmapImageFormat(templateCode, constraints, fileFormat, inputStream);

            if (constraints.SupportedImageSizes.All(x => imageInfo.Width != x.Width || imageInfo.Height != x.Height))
            {
                throw new InvalidBinaryException(templateCode, new ImageUnsupportedSizeError(new ImageSize { Height = imageInfo.Height, Width = imageInfo.Width }));
            }
        }

        public static void ValidateScalableBitmapImageHeader(int templateCode, ScalableBitmapImageElementConstraints constraints, FileFormat fileFormat, Stream inputStream)
        {
            var imageInfo = ValidateBitmapImageFormat(templateCode, constraints, fileFormat, inputStream);

            var imageSize = new ImageSize { Width = imageInfo.Width, Height = imageInfo.Height };
            if (!constraints.ImageSizeRange.Includes(imageSize))
            {
                throw new InvalidBinaryException(templateCode, new ImageSizeOutOfRangeError(imageSize));
            }

            if (constraints.ImageAspectRatio.HasValue)
            {
                var aspectRatio = imageInfo.Width / (decimal)imageInfo.Height;
                if (Math.Abs(constraints.ImageAspectRatio.Value - aspectRatio) < AspectRatioEpsilon)
                {
                    throw new InvalidBinaryException(templateCode, new ImageUnsupportedAspectRatioError(aspectRatio));
                }
            }
        }

        public static void ValidateCompositeBitmapImageOriginalHeader(int templateCode, CompositeBitmapImageElementConstraints constraints, FileFormat fileFormat, Stream inputStream)
        {
            var imageInfo = ValidateBitmapImageFormat(templateCode, constraints, fileFormat, inputStream);

            var imageSize = new ImageSize { Width = imageInfo.Width, Height = imageInfo.Height };
            if (!constraints.ImageSizeRange.Includes(imageSize))
            {
                throw new InvalidBinaryException(templateCode, new ImageSizeOutOfRangeError(imageSize));
            }
        }

        public static void ValidateSizeSpecificBitmapImageHeader(
            int templateCode,
            CompositeBitmapImageElementConstraints constraints,
            FileFormat fileFormat,
            Stream inputStream,
            ImageSize targetImageSize)
        {
            var imageInfo = ValidateBitmapImageFormat(templateCode, constraints, fileFormat, inputStream);

            var imageSize = new ImageSize { Width = imageInfo.Width, Height = imageInfo.Height };
            if (imageSize != targetImageSize)
            {
                throw new InvalidBinaryException(templateCode, new SizeSpecificImageTargetSizeNotEqualToActualSizeError(imageSize));
            }

            if (constraints.SizeSpecificImageIsSquare && imageSize.Width != imageSize.Height)
            {
                throw new InvalidBinaryException(templateCode, new SizeSpecificImageIsNotSquareError());
            }
        }

        public static void ValidateBitmapImage(int templateCode, BitmapImageElementConstraints constraints, Stream inputStream)
        {
            if (!constraints.IsAlphaChannelRequired)
            {
                return;
            }

            Image<Rgba32> decodedImage;
            try
            {
                inputStream.Position = 0;
                decodedImage = Image.Load(inputStream);
            }
            catch (Exception)
            {
                throw new InvalidBinaryException(templateCode, new InvalidImageError());
            }

            if (!IsImageContainsAlphaChannel(decodedImage))
            {
                throw new InvalidBinaryException(templateCode, new ImageMissingAlphaChannelError());
            }
        }

        private static IImageInfo ValidateBitmapImageFormat(int templateCode, IBinaryElementConstraints constraints, FileFormat fileFormat, Stream inputStream)
        {
            var imageFormats =
                constraints.SupportedFileFormats
                           .Aggregate(
                               new List<string>(),
                               (result, next) =>
                                   {
                                       if (ImageFormatToMimeTypeMap.TryGetValue(next, out var imageFormat))
                                       {
                                           result.Add(imageFormat);
                                       }

                                       return result;
                                   });

            inputStream.Position = 0;
            var format = Image.DetectFormat(inputStream);
            if (format == null)
            {
                throw new InvalidBinaryException(templateCode, new InvalidImageError());
            }

            var extension = fileFormat.ToString().ToLowerInvariant();
            if (!format.FileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                // Image format is not consistent with filename extension
                throw new InvalidBinaryException(templateCode, new BinaryExtensionMismatchContentError(extension, format.Name.ToLowerInvariant()));
            }

            if (!imageFormats.Contains(format.DefaultMimeType, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidBinaryException(templateCode, new BinaryInvalidFormatError(format.Name.ToLowerInvariant()));
            }

            IImageInfo imageInfo;
            try
            {
                inputStream.Position = 0;
                imageInfo = Image.Identify(inputStream);
            }
            catch (Exception)
            {
                throw new InvalidBinaryException(templateCode, new InvalidImageError());
            }

            return imageInfo;
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
