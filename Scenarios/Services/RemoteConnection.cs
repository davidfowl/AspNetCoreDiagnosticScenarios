using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Scenarios.Services
{
    /// <summary>
    /// This represents a remote connection to something. Unfortunately Dispose is still problematic but that will be fixed with
    /// IAsyncDisposable. In the mean time, to dispose something on shutdown, we'd need to block in the dispose implementation (but that happens off request threads).
    /// </summary>
    public interface IRemoteConnection
    {
        Task PublishAsync(string channel, string message);
        Task DisposeAsync();
    }

    public class RemoteConnection : IRemoteConnection
    {
        public Task PublishAsync(string channel, string message)
        {
            return Task.CompletedTask;
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    public class RemoteConnectionFactory
    {
        // Configurtion would be used to read the connection information
        private readonly IConfiguration _configuration;

        public RemoteConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// This factory method creates a new connection every time after connecting to that remote end point
        /// </summary>
        public async Task<RemoteConnection> ConnectAsync()
        {
            var random = new Random();

            // Fake delay to a remote connection
            await Task.Delay(random.Next(10) * 1000);

            return new RemoteConnection();
        }
    }

    /// <summary>
    /// This implementation uses the RemoteConnectionFactory and lazily initializes the connection when operations happen
    /// </summary>
    public class LazyRemoteConnection : IRemoteConnection
    {
        private readonly AsyncLazy<RemoteConnection> _connectionTask;

        public LazyRemoteConnection(RemoteConnectionFactory remoteConnectionFactory)
        {
            _connectionTask = new AsyncLazy<RemoteConnection>(() => remoteConnectionFactory.ConnectAsync());
        }

        public async Task PublishAsync(string channel, string message)
        {
            var connection = await _connectionTask.Value;

            await connection.PublishAsync(channel, message);
        }

        public async Task DisposeAsync()
        {
            // Don't connect just to dispose
            if (!_connectionTask.IsValueCreated)
            {
                return;
            }

            var connection = await _connectionTask.Value;

            await connection.DisposeAsync();
        }

        private class AsyncLazy<T> : Lazy<Task<T>>
        {
            public AsyncLazy(Func<Task<T>> valueFactory) : base(valueFactory)
            {
            }
        }
    }

    /// <summary>
    /// This connection implementation gets an IRemoteConnection and an ILoggerFactory in the constructor.
    /// It will dead lock the DI resolution process because it will end up waiting on the same lock.
    /// </summary>
    public class LoggingRemoteConnection : IRemoteConnection
    {
        private readonly IRemoteConnection _remoteConnection;
        private readonly ILogger _logger;
        public LoggingRemoteConnection(IRemoteConnection connection, ILogger logger)
        {
            _remoteConnection = connection;
            _logger = logger;
        }

        public Task DisposeAsync()
        {
            _logger.LogInformation("Disposing the remote connection");
            return _remoteConnection.DisposeAsync();
        }

        public Task PublishAsync(string channel, string message)
        {
            _logger.LogInformation("Publishing message={message} to the remote connection on channel {channel}", message, channel);
            return _remoteConnection.PublishAsync(channel, message);
        }
    }
}
