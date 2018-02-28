using System;

namespace NuClear.VStore.Descriptors
{
    public struct ImageAspectRatio
    {
        public int RatioWidth { get; set; }

        public int RatioHeight { get; set; }

        public static bool operator ==(ImageAspectRatio obj1, ImageAspectRatio obj2)
        {
            return obj1.RatioHeight == obj2.RatioHeight && obj1.RatioWidth == obj2.RatioWidth;
        }

        public static bool operator !=(ImageAspectRatio obj1, ImageAspectRatio obj2)
        {
            return !(obj1 == obj2);
        }

        public override bool Equals(object obj)
        {
            return obj is ImageAspectRatio size && this == size;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (RatioWidth * 397) ^ RatioHeight;
            }
        }

        public override string ToString()
        {
            return $"{RatioWidth}:{RatioHeight}";
        }

        public ImageAspectRatio GetNormalized()
        {
            var normalized = new ImageAspectRatio { RatioWidth = RatioWidth, RatioHeight = RatioHeight };
            var gcd = FindGreatestCommonDivisor(RatioWidth, RatioHeight);
            if (gcd > 1)
            {
                normalized.RatioWidth /= gcd;
                normalized.RatioHeight /= gcd;
            }

            return normalized;
        }

        public static explicit operator decimal(ImageAspectRatio aspectRatio)
        {
            if (aspectRatio.RatioHeight == 0)
            {
                throw new InvalidOperationException();
            }

            return aspectRatio.RatioWidth / (decimal)aspectRatio.RatioHeight;
        }

        private static int FindGreatestCommonDivisor(int first, int second)
        {
            var a = Math.Abs(first);
            var b = Math.Abs(second);
            while (a != 0 && b != 0)
            {
                if (a > b)
                {
                    a %= b;
                }
                else
                {
                    b %= a;
                }
            }

            return a == 0 ? b : a;
        }
    }
}