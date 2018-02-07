using System;

namespace NuClear.VStore.ImageRendering
{
    public sealed class MemoryLimitedException : Exception
    {
        public MemoryLimitedException(int memoryRequested, int memoryThreshold)
            : base($"Memory limits applied. Requested memory: '{memoryRequested}', memory threshold: '{memoryThreshold}'.")
        {
        }
    }
}