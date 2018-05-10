using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NuClear.VStore.Options;

using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;

using StackExchange.Redis;

namespace NuClear.VStore.Locks
{
    public sealed class ReliableRedLockFactory : IDistributedLockFactory, IDisposable
    {
        private const int DefaultConnectionTimeout = 1000;
        private const int DefaultSyncTimeout = 1000;
        private const int DefaultKeepAlive = 1;

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ReliableRedLockFactory> _logger;

        private RedLockFactory _innerFactory;

        public ReliableRedLockFactory(DistributedLockOptions lockOptions, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            _logger = _loggerFactory.CreateLogger<ReliableRedLockFactory>();
            RunRedLockFactoryResilienceTask(lockOptions);
        }

        public IRedLock CreateLock(string resource, TimeSpan expiryTime)
        {
            try
            {
                _lock.EnterReadLock();
                return _innerFactory.CreateLock(resource, expiryTime);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task<IRedLock> CreateLockAsync(string resource, TimeSpan expiryTime)
        {
            try
            {
                _lock.EnterReadLock();
                return _innerFactory.CreateLockAsync(resource, expiryTime);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IRedLock CreateLock(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, CancellationToken? cancellationToken = null)
        {
            try
            {
                _lock.EnterReadLock();
                return _innerFactory.CreateLock(resource, expiryTime, waitTime, retryTime, cancellationToken);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task<IRedLock> CreateLockAsync(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, CancellationToken? cancellationToken = null)
        {
            try
            {
                _lock.EnterReadLock();
                return _innerFactory.CreateLockAsync(resource, expiryTime, waitTime, retryTime, cancellationToken);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _lock.Dispose();
            _innerFactory?.Dispose();
        }

        private static (int, IList<RedLockMultiplexer>) Initialize(DistributedLockOptions lockOptions, ILogger logger)
        {
            var endpoints = lockOptions.GetEndPoints();
            if (endpoints == null || !endpoints.Any())
            {
                throw new ArgumentException("No endpoints specified.");
            }

            var multiplexers = new List<RedLockMultiplexer>(endpoints.Count);
            var keepAlive = lockOptions.KeepAlive ?? DefaultKeepAlive;
            var logWriter = new LogWriter(logger);
            foreach (var endpoint in endpoints)
            {
                logger.LogInformation(
                    "{host}:{port} will be used as RedLock endpoint.",
                    endpoint.Host,
                    endpoint.Port);

                var redisConfig = new ConfigurationOptions
                    {
                        DefaultVersion = new Version(4, 0),
                        AbortOnConnectFail = false,
                        EndPoints = { new DnsEndPoint(endpoint.Host, endpoint.Port) },
                        CommandMap = CommandMap.Create(new HashSet<string> { "SUBSCRIBE" }, available: false),
                        Password = lockOptions.Password,
                        ConnectTimeout = lockOptions.ConnectionTimeout ?? DefaultConnectionTimeout,
                        SyncTimeout = lockOptions.SyncTimeout ?? DefaultSyncTimeout,
                        KeepAlive = keepAlive,
                        // Time (seconds) to check configuration. This serves as a keep-alive for interactive sockets, if it is supported.
                        ConfigCheckSeconds = keepAlive
                    };

                var multiplexer = ConnectionMultiplexer.Connect(redisConfig, logWriter);
                multiplexer.ConnectionFailed +=
                    (sender, args) =>
                        {
                            logger.LogWarning(
                                args.Exception,
                                "ConnectionFailed: {endpoint} ConnectionType: {connectionType} FailureType: {failureType}",
                                GetFriendlyName(args.EndPoint),
                                args.ConnectionType,
                                args.FailureType);
                        };

                multiplexer.ConnectionRestored +=
                    (sender, args) =>
                        {
                            logger.LogWarning(
                                args.Exception,
                                "ConnectionRestored: {endpoint} ConnectionType: {connectionType} FailureType: {failureType}",
                                GetFriendlyName(args.EndPoint),
                                args.ConnectionType,
                                args.FailureType);
                        };

                multiplexer.InternalError +=
                    (sender, args) =>
                        {
                            logger.LogWarning(
                                args.Exception,
                                "InternalError: {endpoint} ConnectionType: {connectionType} Origin: {origin}",
                                GetFriendlyName(args.EndPoint),
                                args.ConnectionType,
                                args.Origin);
                        };

                multiplexer.ErrorMessage +=
                    (sender, args) =>
                        {
                            logger.LogWarning("ErrorMessage: {endpoint} Message: {message}", GetFriendlyName(args.EndPoint), args.Message);
                        };

                multiplexers.Add(multiplexer);
            }

            return (keepAlive, multiplexers);
        }

        private static string GetFriendlyName(EndPoint endPoint)
        {
            switch (endPoint)
            {
                case DnsEndPoint dnsEndPoint:
                    return $"{dnsEndPoint.Host}:{dnsEndPoint.Port}";
                case IPEndPoint ipEndPoint:
                    return $"{ipEndPoint.Address}:{ipEndPoint.Port}";
            }

            return endPoint.ToString();
        }

        private void RunRedLockFactoryResilienceTask(DistributedLockOptions lockOptions)
        {
            Task.Factory.StartNew(
                async () =>
                    {
                        var timeout = DefaultKeepAlive;
                        var connections = new List<(EndPoint EndPoint, IConnectionMultiplexer Multiplexer)>();

                        var recreationNeeded = true;
                        while (!_cancellationTokenSource.IsCancellationRequested)
                        {
                            if (recreationNeeded)
                            {
                                try
                                {
                                    var (keepAlive, multiplexers) = Initialize(lockOptions, _logger);
                                    timeout = keepAlive;
                                    connections = multiplexers
                                                  .Select(x => (x.ConnectionMultiplexer.GetEndPoints()[0], x.ConnectionMultiplexer))
                                                  .ToList();

                                    _lock.EnterWriteLock();
                                    if (_innerFactory == null)
                                    {
                                        _innerFactory = RedLockFactory.Create(multiplexers, _loggerFactory);
                                    }
                                    else
                                    {
                                        var oldFactory = _innerFactory;
                                        using (oldFactory)
                                        {
                                            _innerFactory = RedLockFactory.Create(multiplexers, _loggerFactory);
                                        }
                                    }

                                    recreationNeeded = false;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Unexpected error occured while connecting to Redis servers. Will try again.");
                                }
                                finally
                                {
                                    _lock.ExitWriteLock();
                                }
                            }

                            for (var i = 0; i < connections.Count; ++i)
                            {
                                var endpoint = connections[i].EndPoint;
                                var multiplexer = connections[i].Multiplexer;
                                try
                                {
                                    _logger.LogTrace("Cheking endpoint {endpoint} for availablity.", GetFriendlyName(endpoint));
                                    var server = multiplexer.GetServer(endpoint);
                                    server.Ping();
                                    _logger.LogTrace("Cheking endpoint {endpoint} is available.", GetFriendlyName(endpoint));
                                }
                                catch
                                {
                                    _logger.LogWarning("RedLock endpoint {endpoint} is unavailable.`All connections will be recreated.", GetFriendlyName(endpoint));
                                    recreationNeeded = true;
                                }
                            }

                            await Task.Delay(TimeSpan.FromSeconds(timeout));
                        }
                    },
                _cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private class LogWriter : TextWriter
        {
            private readonly ILogger _logger;

            public LogWriter(ILogger logger)
            {
                _logger = logger;
            }

            public override Encoding Encoding => Encoding.UTF8;

            public override void WriteLine(string value) => _logger.LogTrace(value);
            public override void WriteLine(string format, object arg0) => _logger.LogTrace(format, arg0);
            public override void WriteLine(string format, params object[] arg) => _logger.LogTrace(format, arg);
        }
    }
}