using System;

namespace AmsMigrator
{
    [Flags]
    public enum ImportTarget
    {
        None = 0,
        CompanyLogotypes = 1,
        KbLogotypes = 2,
        ZmkBrendingLogotypes = 4,
        Moderation = 8,
        LogoAndKb = CompanyLogotypes | KbLogotypes,
        LogoAndZmk = CompanyLogotypes | ZmkBrendingLogotypes,
        All = CompanyLogotypes | KbLogotypes | ZmkBrendingLogotypes
    }

    public enum AdExportMode
    {
        OnePerTime, Batch
    }
}
