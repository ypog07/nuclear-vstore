using System;
using System.Collections.Generic;

namespace NuClear.VStore.Descriptors.Objects
{
    public sealed class CompositeBitmapImageElementValue : ICompositeBitmapImageElementValue
    {
        public string Raw { get;  set; }
        public string Filename { get; set; }
        public long? Filesize { get; set;  }

        public Uri DownloadUri { get; set; }
        public Uri PreviewUri { get; set; }

        public CropArea CropArea { get; set; }
        public IEnumerable<SizeSpecificImage> SizeSpecificImages { get; set; }
    }
}