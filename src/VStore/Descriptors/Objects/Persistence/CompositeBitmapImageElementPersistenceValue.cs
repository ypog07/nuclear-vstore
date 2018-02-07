using System.Collections.Generic;

namespace NuClear.VStore.Descriptors.Objects.Persistence
{
    public class CompositeBitmapImageElementPersistenceValue : IBinaryElementPersistenceValue
    {
        public CompositeBitmapImageElementPersistenceValue(
            string raw,
            string filename,
            long? filesize,
            CropArea cropArea,
            IEnumerable<SizeSpecificImage> sizeSpecificImages)
        {
            Raw = raw;
            Filename = filename;
            Filesize = filesize;
            CropArea = cropArea;
            SizeSpecificImages = sizeSpecificImages;
        }

        public string Raw { get; }
        public string Filename { get; }
        public long? Filesize { get; }

        public CropArea CropArea { get; set; }
        public IEnumerable<SizeSpecificImage> SizeSpecificImages { get; set; }

        public class SizeSpecificImage
        {
            public ImageSize Size { get; set; }
            public string Raw { get; set; }

            public string Filename { get; set; }
            public long? Filesize { get; set; }
        }
    }
}