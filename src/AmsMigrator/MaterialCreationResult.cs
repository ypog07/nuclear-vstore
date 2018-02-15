namespace AmsMigrator
{
    public class MaterialCreationResult
    {
        public string VersionId { get; set; }
        public long MaterialId { get; set; }
        public long FirmId { get; set; }
        public long[] BindedNomenclatures { get; set; }
    }
}
