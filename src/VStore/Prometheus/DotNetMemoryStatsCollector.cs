using System.Diagnostics;

using Prometheus.Client;
using Prometheus.Client.Collectors;

namespace NuClear.VStore.Prometheus
{
    public sealed class DotNetMemoryStatsCollector : IOnDemandCollector
    {
        private readonly MetricFactory _metricFactory;
        private readonly Process _process;

        private Counter _memoryCollectionErrors;
        private Gauge _privateMemorySize64;
        private Gauge _virtualMemorySize64;
        private Gauge _workingSet64;

        public DotNetMemoryStatsCollector()
            : this(Metrics.DefaultFactory)
        {
        }

        public DotNetMemoryStatsCollector(MetricFactory metricFactory)
        {
            _metricFactory = metricFactory;
            _process = Process.GetCurrentProcess();
        }

        public void RegisterMetrics()
        {
            _memoryCollectionErrors = _metricFactory.CreateCounter("dotnet_memory_collection_errors_total", "Total number of errors that occured during collections");
            _privateMemorySize64 = _metricFactory.CreateGauge("dotnet_memory_private_memory_size_64", "The value of Process.PrivateMemorySize64");
            _virtualMemorySize64 = _metricFactory.CreateGauge("dotnet_memory_virtual_memory_size_64", "The value of Process.VirtualMemorySize64");
            _workingSet64 = _metricFactory.CreateGauge("dotnet_memory_working_set_64", "The value of Process.WorkingSet64");
        }

        public void UpdateMetrics()
        {
            try
            {
                _process.Refresh();

                _privateMemorySize64.Set(_process.PrivateMemorySize64);
                _virtualMemorySize64.Set(_process.VirtualMemorySize64);
                _workingSet64.Set(_process.WorkingSet64);
            }
            catch
            {
                _memoryCollectionErrors.Inc();
            }
        }
    }
}