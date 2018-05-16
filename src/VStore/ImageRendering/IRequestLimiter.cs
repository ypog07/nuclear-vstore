using System.Threading;
using System.Threading.Tasks;

namespace NuClear.VStore.ImageRendering
{
    public interface IRequestLimiter
    {
        Task HandleRequestAsync(int requiredMemoryInBytes, CancellationToken cancellationToken);
    }
}