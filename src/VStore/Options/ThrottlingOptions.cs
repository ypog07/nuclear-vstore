using System;

namespace NuClear.VStore.Options
{
    public sealed class ThrottlingOptions
    {
        public TimeSpan RequestTimeout { get; set; }
        public TimeSpan RetryAfter { get; set; }
        public long MemoryLimit { get; set; }
        public float ThresholdFactor { get; set; }
    }
}