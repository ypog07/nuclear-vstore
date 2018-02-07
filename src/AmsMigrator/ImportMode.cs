using System;

namespace AmsMigrator
{
    [Flags]
    public enum ImportTarget { None = 0, Logotypes = 1, KbLogotypes = 2, ZmkBrending = 4, LogoAndZmk = Logotypes | ZmkBrending, All = Logotypes | KbLogotypes | ZmkBrending }

    public enum AdExportMode
    {
        OnePerTime, Batch
    }
}
