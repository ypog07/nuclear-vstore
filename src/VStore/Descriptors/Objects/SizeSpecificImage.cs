using System;

namespace NuClear.VStore.Descriptors.Objects
{
    public class SizeSpecificImage
    {
        public ImageSize Size { get; set; }
        public string Raw { get; set; }
        public Uri DownloadUri { get; set; }

        public string Filename { get; set; }
        public long? Filesize { get; set; }
    }
}