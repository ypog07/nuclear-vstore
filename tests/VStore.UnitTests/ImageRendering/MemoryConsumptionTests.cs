using System;
using System.IO;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using Xunit;

namespace VStore.UnitTests.ImageRendering
{
    public sealed class MemoryConsumptionTests
    {
        [Fact]
        public void ShouldBeAllocatedLessThen80Mb()
        {
            const int MemoryForPixels = 5000 * 3750 * 4;
            const double MemoryLowerBound = MemoryForPixels - MemoryForPixels * 0.05;
            const double MemoryUpperBound = MemoryForPixels + MemoryForPixels * 0.05;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetTotalMemory(false);
            using (Image.Load<Rgba32>(Path.Combine("images", "5000x3750.png")))
            {
                var after = GC.GetTotalMemory(false);

                Assert.InRange(after - before, MemoryLowerBound, MemoryUpperBound);
            }
        }

        [Fact]
        public void ShouldBeAllocatedLessThen160Mb()
        {
            const int MemoryForPixels = 5000 * 3750 * 4;
            const double MemoryLowerBound = 2 * (MemoryForPixels - MemoryForPixels * 0.05);
            const double MemoryUpperBound = 2 * (MemoryForPixels + MemoryForPixels * 0.05);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetTotalMemory(false);
            using (Image.Load<Rgba32>(Path.Combine("images", "5000x3750.png")))
            {
                using (Image.Load<Rgba32>(Path.Combine("images", "5000x3750.png")))
                {
                    var after = GC.GetTotalMemory(false);

                    Assert.InRange(after - before, MemoryLowerBound, MemoryUpperBound);
                }
            }
        }

        [Fact]
        public void ShouldBeAllocatedLessThen4Mb()
        {
            const int RentedMemory = 1024 * 1024 * 4;
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

            var after = GC.GetTotalMemory(false);

            Assert.True(after - before < RentedMemory);
        }

        [Fact]
        public void ShouldBeAllocatedLessThen4MbNested()
        {
            const int RentedMemory = 1024 * 1024 * 4;
            const int LoadCount = 10;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetTotalMemory(false);

            for (var index = 0; index < LoadCount; index++)
            {
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
                                    var after = GC.GetTotalMemory(false);

                                    Assert.True(after - before < RentedMemory);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void ShouldBeAllocatedLessThen84MbNested()
        {
            const int MemoryForPixels = 5000 * 3750 * 4;
            const int RentedMemory = 1024 * 1024 * 4;
            const double MemoryUpperBound = RentedMemory + MemoryForPixels + MemoryForPixels * 0.05;

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
                                    var after = GC.GetTotalMemory(false);

                                    Assert.InRange(after - before, RentedMemory, MemoryUpperBound);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void ShouldBeAllocatedLessThen164MbNested()
        {
            const int MemoryForPixels = 5000 * 3750 * 4;
            const int RentedMemory = 1024 * 1024 * 4;
            const double MemoryLowerBound = RentedMemory + MemoryForPixels;
            const double MemoryUpperBound = RentedMemory + 2 * (MemoryForPixels + MemoryForPixels * 0.05);

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
                                        var after = GC.GetTotalMemory(false);

                                        Assert.InRange(after - before, MemoryLowerBound, MemoryUpperBound);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}