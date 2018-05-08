using System;

namespace NuClear.VStore.Options
{
    public static class CdnOptionsExtensions
    {
        private const string CdnRawsUriPrefix = "raws";
        private const string CdnPreviewsUriPrefix = "previews";

        public static Uri AsRawUri(this CdnOptions options, string fileKey)
        {
            return new Uri(options.CdnUrl, $"{CdnRawsUriPrefix}/{fileKey}");
        }

        public static Uri AsCompositePreviewUri(this CdnOptions options, long objectId, string versionId, long templateCode)
        {
            return new Uri(options.CdnUrl, $"{CdnPreviewsUriPrefix}/{objectId}/{versionId}/{templateCode}/image.png");
        }

        public static Uri AsScalablePreviewUri(this CdnOptions options, long objectId, string versionId, long templateCode)
        {
            return new Uri(options.CdnUrl, $"{CdnPreviewsUriPrefix}/{objectId}/{versionId}/{templateCode}/");
        }
    }
}