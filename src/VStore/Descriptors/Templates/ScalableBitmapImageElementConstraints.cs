using System;
using System.Collections.Generic;
using System.Linq;

namespace NuClear.VStore.Descriptors.Templates
{
    public class ScalableBitmapImageElementConstraints :
        IBinaryElementConstraints,
        IBinaryFormatConstraints,
        ISizeRangeImageElementConstraints,
        IImageElementConstraints,
        IEquatable<ScalableBitmapImageElementConstraints>
    {
        public ImageSizeRange ImageSizeRange { get; set; }

        public int? MaxSize { get; set; }
        public int? MaxFilenameLength { get; set; }
        public IReadOnlyCollection<FileFormat> SupportedFileFormats { get; set; }
        public bool BinaryExists => true;

        public bool ValidImage => true;
        public bool ExtensionMatchContentFormat => true;

        public decimal? ImageAspectRatio { get; set; }

        #region Equality members

        public bool Equals(ScalableBitmapImageElementConstraints other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return ImageSizeRange.Equals(other.ImageSizeRange) &&
                   BinaryExists == other.BinaryExists &&
                   ValidImage == other.ValidImage &&
                   MaxSize == other.MaxSize &&
                   ImageAspectRatio == other.ImageAspectRatio &&
                   MaxFilenameLength == other.MaxFilenameLength &&
                   (ReferenceEquals(SupportedFileFormats, other.SupportedFileFormats) ||
                    SupportedFileFormats.SequenceEqual(other.SupportedFileFormats));
        }

        public override bool Equals(object obj) => Equals(obj as ScalableBitmapImageElementConstraints);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ImageSizeRange.GetHashCode();
                hashCode = (hashCode * 397) ^ BinaryExists.GetHashCode();
                hashCode = (hashCode * 397) ^ ValidImage.GetHashCode();
                hashCode = (hashCode * 397) ^ ExtensionMatchContentFormat.GetHashCode();
                hashCode = (hashCode * 397) ^ ImageAspectRatio.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxSize.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxFilenameLength.GetHashCode();
                return hashCode;
            }
        }

        #endregion
    }
}