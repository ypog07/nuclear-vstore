namespace NuClear.VStore.Descriptors
{
    public struct ImageSize
    {
        public static ImageSize Empty { get; } = new ImageSize();

        public int Width { get; set; }

        public int Height { get; set; }

        public static bool operator ==(ImageSize obj1, ImageSize obj2)
        {
            return obj1.Height == obj2.Height && obj1.Width == obj2.Width;
        }

        public static bool operator !=(ImageSize obj1, ImageSize obj2)
        {
            return !(obj1 == obj2);
        }

        public override bool Equals(object obj)
        {
            return obj is ImageSize && this == (ImageSize)obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Height * 397) ^ Width;
            }
        }

        public static bool TryParse(string size, out ImageSize imageSize)
        {
            imageSize = Empty;
            if (string.IsNullOrEmpty(size))
            {
                return false;
            }

            var sizeTokens = size.Split('x');
            if (sizeTokens.Length != 2 || !int.TryParse(sizeTokens[0], out var width) || !int.TryParse(sizeTokens[1], out var height))
            {
                return false;
            }

            imageSize = new ImageSize { Width = width, Height = height };
            return true;
        }

        public override string ToString()
        {
            return $"{Width}x{Height}";
        }
    }
}