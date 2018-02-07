using System;

namespace AmsMigrator
{
    public class ImportOptions
    {
        public DateTime ThresholdDate { get; set; }

        public string OkApiBaseUri { get; set; }
        public string Amsv1BaseUri { get; set; }
        public string Amsv1AuthToken { get; set; }
        public string OkApiAuthToken { get; set; }
        public string OkApiApiVersion { get; set; }
        public ImportTarget Targets { get; set; }
        public AdExportMode Mode { get; set; }
        public TestModeOptions TestMode { get; set; }
        public int BatchSize { get; set; }
        public bool ParallelImportEnabled { get; set; }
        public string SourceDbConnectionString { get; set; }
        public int? MaxImageSizeLimit { get; set; }
        public long[] KbLogoTriggerNomenclatures { get; set; }
        public long[] ZmkLogoTriggerNomenclatures { get; set; }

        public bool MaterialOrderBindingEnabled { get; set; }

        public long[] ZmkNomenclatureCodes { get; set; }
        public long[] KbNomenclatureCodes { get; set; }
        public long[] LogoNomenclatureCodes { get; set; }
        public long? LogoTemplateCode { get; set; }
        public long? ZmkTemplateCode { get; set; }
        public long? KbTemplateCode { get; set; }
        public MigrationStatus[] StatusesForMigration { get; set; }
        public int[] SizeSpecificImageSizes { get; set; }
        public int HttpServicesRetryCount { get; set; }
        public string Language { get; set; }
        public long? ErmUserId { get; set; }
        public long? StartAfterFirmId { get; set; }
        public string StartFromDb { get; set; }
        public long? StartFromTemplate { get; set; }
        public int QueueTriesCount { get; set; }
        public string[] AmsV1Uuids { get; set; }
        public int? ProjectId { get; set; }
        public string DbInstanceKey { get; set; }
    }

    public class MigrationStatus
    {
        public string Name { get; set; }
        public bool MigrationNeeded { get; set; }
    }

    public class TestModeOptions
    {
        public bool Enabled { get; set; }
        public int Limit { get; set; }
    }
}
