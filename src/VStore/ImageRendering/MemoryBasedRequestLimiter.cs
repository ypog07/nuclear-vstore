using System;
using System.Runtime;
using System.Threading;

using NuClear.VStore.Options;

namespace NuClear.VStore.ImageRendering
{
    public sealed class MemoryBasedRequestLimiter
    {
        private readonly object _syncRoot = new object();
        private readonly int _memoryToAllocateThreshold;

        public MemoryBasedRequestLimiter(ThrottlingOptions throttlingOptions)
        {
            _memoryToAllocateThreshold = (int)(0.5 * throttlingOptions.MemoryLimit);
        }

        public void HandleRequest(int requiredMemoryInBytes, CancellationToken cancellationToken)
        {
            lock (_syncRoot)
            {
                var managedMemory = GC.GetTotalMemory(false);
                if (managedMemory + requiredMemoryInBytes > _memoryToAllocateThreshold)
                {
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    managedMemory = GC.GetTotalMemory(true);
                    if (managedMemory + requiredMemoryInBytes > _memoryToAllocateThreshold)
                    {
                        throw new MemoryLimitedException(requiredMemoryInBytes, _memoryToAllocateThreshold);
                    }
                }
            }
        }
    }
}