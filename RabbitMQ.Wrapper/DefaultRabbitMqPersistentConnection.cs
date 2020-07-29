using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Wrapper.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace RabbitMQ.Wrapper
{
    public class DefaultRabbitMqPersistentConnection : IRabbitMqPersistentConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<DefaultRabbitMqPersistentConnection> _logger;
        private readonly int _retryCount;
        private readonly object _locker = new object();
        private IConnection _connection;
        private bool _disposed = false;

        public DefaultRabbitMqPersistentConnection(IConnectionFactory connectionFactory, ILogger<DefaultRabbitMqPersistentConnection> logger, int retryCount)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _retryCount = retryCount;
        }
        public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

        public IModel CreateModel()
        {
            if (!IsConnected)
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");

            return _connection.CreateModel();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _connection.Dispose();
            }
            catch(IOException ex)
            {
                _logger.LogCritical(ex.Message);
            }
        }

        public bool TryConnect()
        {
            _logger.LogInformation("RabbitMQ Client is trying to connect");

            lock(_locker)
            {
                var policy = Policy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (ex, time) =>
                    {
                        _logger.LogWarning(ex,
                                "RabbitMQ Client could not connect after {TimeOut}s ({ExceptionMessage})",
                                $"{time.TotalSeconds:n1}", ex.Message);
                    });

                _connection = policy.Execute(() => _connectionFactory.CreateConnection());

                if (IsConnected)
                {
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;

                    _logger.LogInformation(
                        "RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events",
                        _connection.Endpoint.HostName);

                    return true;
                }

                _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");
                return false;
            }
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

            TryConnect();
        }

        private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

            TryConnect();
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

            TryConnect();
        }
    }
}
