using System.Linq;

namespace AmsMigrator.Models
{
    public class Amsv1MaterialData
    {
        public string Uuid { get; set; }
        public long FirmId { get; set; }

        public string State { get; set; }

        public string BackgroundColor { get; set; }

        public int CropHeight { get; set; }

        public int CropLeft { get; set; }

        public int CropTop { get; set; }

        public int CropWidth { get; set; }

        public string ImageExt { get; set; }

        public long? ImageHeight { get; set; }

        public string ImageName { get; set; }

        public long? ImageSize { get; set; }

        public string ImageType { get; set; }

        public string ImageUrl { get; set; }

        public long? ImageWidth { get; set; }

        public byte[] ImageDataOriginal { get; set; }

        public byte[] ImageData { get; set; }

        public string ModerationComment { get; set; }

        public string ModerationState { get; set; }

        public bool HasModerationInfo { get; set; }

        public SizeSpecificImageData[] SizeSpecificImages { get; set; }

        public bool HasSizeSpecificImages => SizeSpecificImages?.Any() ?? false;

        public ImportTarget ReachedTarget { get; set; }

        public void Complete(ImportTarget target)
        {
            ReachedTarget |= target;
        }

        public int QueueTries { get; set; }

        public override string ToString()
        {
            return $"Url: {ImageUrl}; Type: {ImageExt}; Size: {ImageSize / 1024} KB; WxH: {ImageWidth}x{ImageHeight}";
        }
    }
}
