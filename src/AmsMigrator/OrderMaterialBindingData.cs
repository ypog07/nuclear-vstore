namespace AmsMigrator
{
    public class OrderMaterialBindingData
    {
        public long MaterialId { get; set; }
        public long FirmId { get; set; }
        public long[] BindedNomenclatures { get; set; }
    }
}
