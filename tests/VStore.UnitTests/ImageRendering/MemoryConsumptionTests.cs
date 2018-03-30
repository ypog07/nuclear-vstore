using System;
using System.IO;
using System.Runtime;

using NuClear.VStore.ImageRendering;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using Xunit;
using Xunit.Abstractions;

namespace VStore.UnitTests.ImageRendering
{
    public sealed class MemoryConsumptionTests
    {
        private readonly ITestOutputHelper _output;

        public MemoryConsumptionTests(ITestOutputHelper output)
        {
            _output = output;
            Configuration.Default.MemoryManager = ArrayPoolMemoryManagerFactory.CreateWithLimitedPooling();
        }

        [Fact]
        public void ShouldBeAllocatedLessThan80Mb()
        {
            const int MemoryForPixels = 5000 * 3750 * 4;
            const double MemoryUpperBound = MemoryForPixels + MemoryForPixels * 0.05;
            var rentedMemory = GetApproximateRentedMemorySize();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetTotalMemory(false);
            using (Image.Load<Rgba32>(Path.Combine("images", "5000x3750.png")))
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                var after = GC.GetTotalMemory(true);

                _output.WriteLine($"{nameof(ShouldBeAllocatedLessThan80Mb)}: {after - before} bytes allocated");
                Assert.InRange(after - before, rentedMemory, MemoryUpperBound);
            }
        }

        [Fact]
        public void ShouldBeAllocatedLessThan160Mb()
        {
            var rentedMemory = GetApproximateRentedMemorySize();
            const int MemoryForPixels = 5000 * 3750 * 4;
            const double MemoryUpperBound = 2 * (MemoryForPixels + MemoryForPixels * 0.05);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetTotalMemory(false);
            using (Image.Load<Rgba32>(Configuration.Default, Path.Combine("images", "5000x3750.png")))
            {
                using (Image.Load<Rgba32>(Path.Combine("images", "5000x3750.png")))
                {
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    var after = GC.GetTotalMemory(true);

                    _output.WriteLine($"{nameof(ShouldBeAllocatedLessThan160Mb)}: {after - before} bytes allocated");
                    Assert.InRange(after - before, rentedMemory, MemoryUpperBound);
                }
            }
        }

        [Fact]
        public void ShouldBeAllocatedLessThan1Mb()
        {
            var rentedMemory = GetApproximateRentedMemorySize();
            const int LoadCount = 10;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetTotalMemory(false);

            for (var index = 0; index < LoadCount; index++)
            {
                using (Image.Load<Rgba32>(Path.Combine("images", "64x48.png")))
                {
                }
            }

            for (var index = 0; index < LoadCount; index++)
            {
                using (Image.Load<Rgba32>(Path.Combine("images", "100x75.png")))
                {
                }
            }

            for (var index = 0; index < LoadCount; index++)
            {
                using (Image.Load<Rgba32>(Path.Combine("images", "500x375.png")))
                {
                }
            }

            for (var index = 0; index < LoadCount; index++)
            {
                using (Image.Load<Rgba32>(Path.Combine("images", "200x150.png")))
                {
                }
            }

            for (var index = 0; index < LoadCount; index++)
            {
                using (Image.Load<Rgba32>(Path.Combine("images", "300x225.png")))
                {
                }
            }

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            var after = GC.GetTotalMemory(true);

            _output.WriteLine($"{nameof(ShouldBeAllocatedLessThan1Mb)}: {after - before} bytes allocated");
            Assert.InRange(after - before, 0, rentedMemory);
        }

        [Fact]
        public void ShouldBeAllocatedLessThan80MbNested()
        {
            const int MemoryForPixels = 5000 * 3750 * 4;
            var rentedMemory = GetApproximateRentedMemorySize();
            var memoryUpperBound = rentedMemory + MemoryForPixels + MemoryForPixels * 0.05;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetTotalMemory(false);

            using (Image.Load<Rgba32>(Path.Combine("images", "64x48.png")))
            {
                using (Image.Load<Rgba32>(Path.Combine("images", "100x75.png")))
                {
                    using (Image.Load<Rgba32>(Path.Combine("images", "500x375.png")))
                    {
                        using (Image.Load<Rgba32>(Path.Combine("images", "200x150.png")))
                        {
                            using (Image.Load<Rgba32>(Path.Combine("images", "300x225.png")))
                            {
                                using (Image.Load<Rgba32>(Path.Combine("images", "5000x3750.png")))
                                {
                                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                                    var after = GC.GetTotalMemory(true);

                                    _output.WriteLine($"{nameof(ShouldBeAllocatedLessThan80MbNested)}: {after - before} bytes allocated");
                                    Assert.InRange(after - before, rentedMemory, memoryUpperBound);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void ShouldBeAllocatedLessThan154MbNested()
        {
            const int MemoryForPixels = 5000 * 3750 * 4;
            var rentedMemory = GetApproximateRentedMemorySize();
            var memoryLowerBound = rentedMemory + MemoryForPixels;
            var memoryUpperBound = rentedMemory + 2 * (MemoryForPixels + MemoryForPixels * 0.05);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetTotalMemory(false);

            using (Image.Load<Rgba32>(Path.Combine("images", "64x48.png")))
            {
                using (Image.Load<Rgba32>(Path.Combine("images", "100x75.png")))
                {
                    using (Image.Load<Rgba32>(Path.Combine("images", "500x375.png")))
                    {
                        using (Image.Load<Rgba32>(Path.Combine("images", "200x150.png")))
                        {
                            using (Image.Load<Rgba32>(Path.Combine("images", "300x225.png")))
                            {
                                using (Image.Load<Rgba32>(Path.Combine("images", "5000x3750.png")))
                                {
                                    using (Image.Load<Rgba32>(Path.Combine("images", "5000x3750.png")))
                                    {
                                        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                                        var after = GC.GetTotalMemory(true);

                                        _output.WriteLine($"{nameof(ShouldBeAllocatedLessThan154MbNested)}: {after - before} bytes allocated");
                                        Assert.InRange(after - before, memoryLowerBound, memoryUpperBound);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static long GetApproximateRentedMemorySize()
        {
            const int MaxPoolSizeInBytes = 350 * 350 * 4;
            const float FluctuationFactor = 1.6f;

            // NOTE: Subject to change!
            // Copy-pasted from https://github.com/dotnet/corefx/blob/master/src/System.Buffers/src/System/Buffers/Utilities.cs#L13
            int GetArrayPoolBucketsQuantity(int bufferSize)
            {
                uint bitsRemaining = ((uint)bufferSize - 1) >> 4;

                int poolIndex = 0;
                if (bitsRemaining > 0xFFFF) { bitsRemaining >>= 16; poolIndex = 16; }
                if (bitsRemaining > 0xFF)   { bitsRemaining >>= 8;  poolIndex += 8; }
                if (bitsRemaining > 0xF)    { bitsRemaining >>= 4;  poolIndex += 4; }
                if (bitsRemaining > 0x3)    { bitsRemaining >>= 2;  poolIndex += 2; }
                if (bitsRemaining > 0x1)    { bitsRemaining >>= 1;  poolIndex += 1; }

                return poolIndex + (int)bitsRemaining;
            }

            long rentedMemorySize = 0;
            for (var i = 0; i < GetArrayPoolBucketsQuantity(MaxPoolSizeInBytes); i++)
            {
                // NOTE: Subject to change!
                // Copy-pasted from https://github.com/dotnet/corefx/blob/master/src/System.Buffers/src/System/Buffers/Utilities.cs#L32
                rentedMemorySize += 16 << i;
            }

            return (long)(FluctuationFactor * rentedMemorySize);
        }
    }
}