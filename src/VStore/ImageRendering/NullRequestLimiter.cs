using System.Threading;
using System.Threading.Tasks;

namespace NuClear.VStore.ImageRendering
{
    public sealed class NullRequestLimiter : IRequestLimiter
    {
        public Task HandleRequestAsync(int requiredMemoryInBytes, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}