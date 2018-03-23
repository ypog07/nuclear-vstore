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
            return new ArrayPoolMemoryManager(1024 * 1024, 32 * 1024, 1, 1);
        }
    }
}