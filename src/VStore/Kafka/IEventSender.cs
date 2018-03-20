using System.Threading.Tasks;

using NuClear.VStore.Events;

namespace NuClear.VStore.Kafka
{
    public interface IEventSender
    {
        Task SendAsync(string topic, IEvent @event);
    }
}