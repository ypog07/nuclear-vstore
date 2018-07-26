using System;

namespace CloningTool
{
    [Flags]
    public enum CloneMode
    {
        CloneTemplates = 1,
        CloneAdvertisements = 2,
        CloneContentPositionsLinks = 4,
        CloneTemplatesWithLinks = CloneTemplates | CloneContentPositionsLinks,
        CloneAll = CloneTemplates | CloneContentPositionsLinks | CloneRemarksWithCategories | CloneAdvertisements,
        TruncatedCloneAdvertisements = 8,
        TruncatedCloneAll = CloneTemplates | CloneContentPositionsLinks | CloneRemarksWithCategories | TruncatedCloneAdvertisements,
        ReloadFiles = 16,
        CloneRemarkCategories = 32,
        CloneRemarks = 64,
        CloneRemarksWithCategories = CloneRemarkCategories | CloneRemarks
    }
}
