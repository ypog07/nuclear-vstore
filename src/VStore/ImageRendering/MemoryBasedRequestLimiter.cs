﻿using System;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;

using NuClear.VStore.Options;

namespace NuClear.VStore.ImageRendering
{
    public sealed class MemoryBasedRequestLimiter : IRequestLimiter
    {
        private readonly object _syncRoot = new object();
        private readonly long _memoryToAllocateThreshold;

        public MemoryBasedRequestLimiter(ThrottlingOptions throttlingOptions)
        {
            _memoryToAllocateThreshold = (long)(throttlingOptions.ThresholdFactor * throttlingOptions.MemoryLimit);
        }

        public Task HandleRequestAsync(int requiredMemoryInBytes, CancellationToken cancellationToken)
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

            return Task.CompletedTask;
        }
    }
}