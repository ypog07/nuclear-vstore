using SixLabors.ImageSharp.Memory;

namespace NuClear.VStore.ImageRendering
{
    public static class ArrayPoolMemoryManagerFactory
    {
        /// <summary>
        /// Similar to <see cref="ArrayPoolMemoryManager.CreateWithModeratePooling"/> option, but with one bucket per every pool only
        /// </summary>
        /// <returns>The memory manager</returns>
        public static ArrayPoolMemoryManager CreateWithLimitedPooling()
        {
            return new ArrayPoolMemoryManager(350 * 350 * 4, 1, 1, 1);
        }

        /// <summary>
        /// Similar to <see cref="ArrayPoolMemoryManager.CreateWithAggressivePooling"/> option, but with less amount of buckets per every pool
        /// </summary>
        /// <returns>The memory manager</returns>
        public static ArrayPoolMemoryManager CreateWithUnlimitedPooling()
        {
            return new ArrayPoolMemoryManager(128 * 1024 * 1024 * 4, 8 * 1024 * 1024 * 4, 4, 16);
        }
    }
}