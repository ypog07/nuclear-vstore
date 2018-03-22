using System;

namespace NuClear.VStore.Descriptors.Objects
{
    public sealed class ScalableBitmapImageElementValue : IScalableBitmapImageElementValue
    {
        public string Raw { get; set; }
        public string Filename { get; set; }
        public long? Filesize { get; set; }

        public Uri DownloadUri { get; set; }
        public Uri PreviewUri { get; set; }

        public Anchor Anchor { get; set; }
    }
}