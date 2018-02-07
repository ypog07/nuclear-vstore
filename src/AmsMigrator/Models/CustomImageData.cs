namespace AmsMigrator.Models
{
    public class SizeSpecificImageData
    {
        public string Name { get; set; }
        public int? Height { get; set; }
        public int? Width { get; set; }
        public string Url { get; set; }
        public byte[] Data { get; set; }
    }
}
