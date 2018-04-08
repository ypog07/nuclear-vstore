namespace NuClear.VStore.Worker
{
    public interface IMetricServer
    {
        bool IsRunning { get; }

        void Start();
        void Stop();
    }
}
