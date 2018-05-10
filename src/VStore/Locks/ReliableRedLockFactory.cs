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

        private static ILogger<ConnectionMultiplexer> _redisLogger;

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ILoggerFactory _loggerFactory;

        private RedLockFactory _innerFactory;
        private IList<IConnectionMultiplexer> _multiplexers = new List<IConnectionMultiplexer>();

        public ReliableRedLockFactory(DistributedLockOptions lockOptions, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            _redisLogger = _loggerFactory.CreateLogger<ConnectionMultiplexer>();

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

        public IRedLock CreateLock(
            string resource,
            TimeSpan expiryTime,
            TimeSpan waitTime,
            TimeSpan retryTime,
            CancellationToken? cancellationToken = null)
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

        public Task<IRedLock> CreateLockAsync(
            string resource,
            TimeSpan expiryTime,
            TimeSpan waitTime,
            TimeSpan retryTime,
            CancellationToken? cancellationToken = null)
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

            foreach (var multiplexer in _multiplexers)
            {
                multiplexer.Dispose();
            }

            _innerFactory?.Dispose();
        }

        private static (int, IList<IConnectionMultiplexer>) Initialize(DistributedLockOptions lockOptions)
        {
            var endpoints = lockOptions.GetEndPoints();
            if (endpoints == null || !endpoints.Any())
            {
                throw new ArgumentException("No endpoints specified.");
            }

            var multiplexers = new List<IConnectionMultiplexer>(endpoints.Count);
            var keepAlive = lockOptions.KeepAlive ?? DefaultKeepAlive;
            var logWriter = new LogWriter(_redisLogger);
            foreach (var endpoint in endpoints)
            {
                _redisLogger.LogInformation(
                    "{host}:{port} will be used as RedLock endpoint.",
                    endpoint.Host,
                    endpoint.Port);

                var redisConfig = new ConfigurationOptions
                    {
                        DefaultVersion = new Version(4, 0),
                        AbortOnConnectFail = true,
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
                multiplexer.ConnectionFailed += OnConnectionFailed;
                multiplexer.ConnectionRestored += OnConnectionRestored;
                multiplexer.InternalError += OnInternalError;
                multiplexer.ErrorMessage += OnInternalError;

                multiplexers.Add(multiplexer);
            }

            return (keepAlive, multiplexers);
        }

        private static void OnConnectionFailed(object sender, ConnectionFailedEventArgs args)
            => _redisLogger.LogWarning(
                args.Exception,
                "ConnectionFailed: {endpoint} ConnectionType: {connectionType} FailureType: {failureType}",
                GetFriendlyName(args.EndPoint),
                args.ConnectionType,
                args.FailureType);

        private static void OnConnectionRestored(object sender, ConnectionFailedEventArgs args)
            => _redisLogger.LogWarning(
                args.Exception,
                "ConnectionRestored: {endpoint} ConnectionType: {connectionType} FailureType: {failureType}",
                GetFriendlyName(args.EndPoint),
                args.ConnectionType,
                args.FailureType);

        private static void OnInternalError(object sender, InternalErrorEventArgs args)
            => _redisLogger.LogWarning(
                args.Exception,
                "InternalError: {endpoint} ConnectionType: {connectionType} Origin: {origin}",
                GetFriendlyName(args.EndPoint),
                args.ConnectionType,
                args.Origin);

        private static void OnInternalError(object sender, RedisErrorEventArgs args)
            => _redisLogger.LogWarning("ErrorMessage: {endpoint} Message: {message}", GetFriendlyName(args.EndPoint), args.Message);

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
                        var logger = _loggerFactory.CreateLogger<ReliableRedLockFactory>();
                        var timeout = DefaultKeepAlive;
                        var redLockMultiplexers = new List<RedLockMultiplexer>();

                        var recreationNeeded = true;
                        while (!_cancellationTokenSource.IsCancellationRequested)
                        {
                            if (recreationNeeded)
                            {
                                try
                                {
                                    _lock.EnterWriteLock();

                                    var (keepAlive, multiplexers) = Initialize(lockOptions);

                                    timeout = keepAlive;
                                    redLockMultiplexers = multiplexers.Select(x => new RedLockMultiplexer(x)).ToList();

                                    if (_innerFactory == null)
                                    {
                                        _innerFactory = RedLockFactory.Create(redLockMultiplexers, _loggerFactory);
                                        _multiplexers = multiplexers;
                                    }
                                    else
                                    {
                                        var oldFactory = _innerFactory;
                                        var oldMultiplexers = _multiplexers;
                                        try
                                        {
                                            _innerFactory = RedLockFactory.Create(redLockMultiplexers, _loggerFactory);
                                            _multiplexers = multiplexers;
                                        }
                                        finally
                                        {
                                            foreach (var multiplexer in oldMultiplexers)
                                            {
                                                var endpoint = multiplexer.GetEndPoints()[0];
                                                logger.LogTrace("Disposing connection to endpoint {endpoint}...", GetFriendlyName(endpoint));

                                                multiplexer.ConnectionFailed -= OnConnectionFailed;
                                                multiplexer.ConnectionRestored -= OnConnectionRestored;
                                                multiplexer.InternalError -= OnInternalError;
                                                multiplexer.ErrorMessage -= OnInternalError;
                                                multiplexer.Dispose();
                                            }

                                            oldFactory.Dispose();
                                        }
                                    }

                                    recreationNeeded = false;
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Unexpected error occured while connecting to Redis servers. Will try again.");
                                }
                                finally
                                {
                                    _lock.ExitWriteLock();
                                }
                            }

                            foreach (var redLockMultiplexer in redLockMultiplexers)
                            {
                                var endpoint = redLockMultiplexer.ConnectionMultiplexer.GetEndPoints()[0];
                                var multiplexer = redLockMultiplexer.ConnectionMultiplexer;
                                try
                                {
                                    logger.LogTrace("Checking RedLock endpoint {endpoint} for availablity.", GetFriendlyName(endpoint));
                                    var server = multiplexer.GetServer(endpoint);
                                    server.Ping();
                                    logger.LogTrace("RedLock endpoint {endpoint} is available.", GetFriendlyName(endpoint));
                                }
                                catch
                                {
                                    logger.LogWarning(
                                        "RedLock endpoint {endpoint} is unavailable. All connections will be recreated.",
                                        GetFriendlyName(endpoint));
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