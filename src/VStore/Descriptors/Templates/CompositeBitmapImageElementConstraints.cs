using System;
using System.Collections.Generic;
using System.Linq;

namespace NuClear.VStore.Descriptors.Templates
{
    public sealed class CompositeBitmapImageElementConstraints :
        IBinaryElementConstraints,
        IImageElementConstraints,
        IEquatable<CompositeBitmapImageElementConstraints>
    {
        public ImageSizeRange ImageSizeRange { get; set; }

        public int? SizeSpecificImageMaxSize { get; set; }
        public bool CropAreaIsSquare => true;
        public bool SizeSpecificImageIsSquare => true;
        public bool SizeSpecificImageTargetSizeEqualToActualSize => true;

        public IReadOnlyCollection<FileFormat> SupportedFileFormats { get; set; }
        public int? MaxSize { get; set; }
        public int? MaxFilenameLength { get; set; }

        public bool BinaryExists => true;
        public bool ValidImage => true;

        #region Equality members

        public bool Equals(CompositeBitmapImageElementConstraints other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return ImageSizeRange.Equals(other.ImageSizeRange) && SizeSpecificImageMaxSize == other.SizeSpecificImageMaxSize
                                                               && MaxSize == other.MaxSize && MaxFilenameLength == other.MaxFilenameLength &&
                                                               (ReferenceEquals(SupportedFileFormats, other.SupportedFileFormats) ||
                                                                SupportedFileFormats.SequenceEqual(other.SupportedFileFormats));
        }

        public override bool Equals(object obj) => Equals(obj as CompositeBitmapImageElementConstraints);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ImageSizeRange.GetHashCode();
                hashCode = (hashCode * 397) ^ SizeSpecificImageMaxSize.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxSize.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxFilenameLength.GetHashCode();
                return hashCode;
            }
        }

        #endregion
    }
}