using Autofac;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Wrapper.Abstractions;
using RabbitMQ.Wrapper.Events;
using RabbitMQ.Wrapper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Wrapper
{
    public class EventBusRabbitMq : IEventBus, IDisposable
    {
        const string BrokerName = "game_event_bus";

        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly ILogger<EventBusRabbitMq> _logger;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly ILifetimeScope _autofac;
        private readonly string _autofacScopeName = "game_event_bus";
        private readonly int _retryCount;

        private IModel _consumerChannel;
        private string _queueName;

        public EventBusRabbitMq(IRabbitMqPersistentConnection persistentConnection, 
            ILogger<EventBusRabbitMq> logger,
            IEventBusSubscriptionsManager subsManager,
            ILifetimeScope autofac,
            string queueName,
            int retryCount)
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subsManager = subsManager ?? throw new ArgumentNullException(nameof(subsManager));
            _autofac = autofac ?? throw new ArgumentNullException(nameof(autofac));
            _queueName = queueName;
            _retryCount = retryCount;
            _consumerChannel = CreateConsumerChannel();
        }
        private IModel CreateConsumerChannel()
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            _logger.LogTrace("Creating RabbitMQ consumer channel");

            var channel = _persistentConnection.CreateModel();

            channel.ExchangeDeclare(exchange: BrokerName, type: "direct");

            channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false,
                arguments: null);

            channel.CallbackException += (sender, ea) =>
            {
                _logger.LogWarning(ea.Exception, "Recreating RabbitMQ consumer channel");

                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
                StartBasicConsume();
            };

            return channel;
        }

        // Publish
        public void SendMessageToQueue(IntegrationEvent @event)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            if (!_persistentConnection.IsConnected)
                _persistentConnection.TryConnect();

            var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, time) =>
                {
                    _logger.LogWarning(ex,
                        "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.Id,
                        $"{time.TotalSeconds:n1}", ex.Message);
                });

            var eventName = @event.GetType().Name;

            _logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", @event.Id,
                eventName);

            using (var channel = _persistentConnection.CreateModel())
            {
                _logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);

                channel.ExchangeDeclare(exchange: BrokerName, type: "direct");

                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);

                policy.Execute(()=> 
                {
                    var properties = channel.CreateBasicProperties();
                    properties.DeliveryMode = 2; // persistent

                    _logger.LogTrace("Publishing event to RabbitMQ: {EventId}", @event.Id);

                    channel.BasicPublish(exchange: BrokerName, routingKey: eventName, mandatory: true,
                        basicProperties: properties, body: body);
                });
            }
        }

        public void SendMessagesToQueue(IEnumerable<IntegrationEvent> @events)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));

            events.ToList().ForEach(SendMessageToQueue);
        }

        // Subscribe
        public void ListenQueue<TEvent, TEventHandler>()
            where TEvent : IntegrationEvent
            where TEventHandler : IIntegrationEventHandler<TEvent>
        {
            var eventName = _subsManager.GetEventKey<TEvent>();
            DoInternalSubscription(eventName);

            _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName,
                typeof(TEventHandler).GetGenericTypeName());

            _subsManager.AddSubscription<TEvent, TEventHandler>();
            StartBasicConsume();
        }
        private void DoInternalSubscription(string eventName)
        {
            var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
            if (!containsKey)
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }

                using (var channel = _persistentConnection.CreateModel())
                {
                    channel.QueueBind(queue: _queueName, exchange: BrokerName, routingKey: eventName);
                }
            }
        }

        private void StartBasicConsume()
        {
            _logger.LogTrace("Starting RabbitMQ basic consume");

            if (_consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

                consumer.Received += Consumer_Received;

                _consumerChannel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
            }
            else
            {
                _logger.LogError("StartBasicConsume can't call on _consumerChannel == null");
            }
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            try
            {
                if (message.ToLowerInvariant().Contains("throw-fake-exception"))
                {
                    throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
                }

                await ProcessEvent(eventName, message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "----- ERROR Processing message \"{Message}\"", message);
            }

            _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false); 
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            _logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);

            if (_subsManager.HasSubscriptionsForEvent(eventName))
            {
                using (var scope = _autofac.BeginLifetimeScope(_autofacScopeName))
                {
                    var subscriptions = _subsManager.GetHandlersForEvent(eventName);
                    foreach (var subscription in subscriptions)
                    {

                        var handler = scope.ResolveOptional(subscription.HandlerType);
                        if (handler == null) continue;
                        var eventType = _subsManager.GetEventTypeByName(eventName);
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);

                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);


                        await Task.Yield();
                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new[] { integrationEvent });
                    }
                }
            }
            else
            {
                _logger.LogWarning("No subscription for RabbitMQ event: {EventName}", eventName);
            }
        }

        public void Unsubscribe<TEvent, TEventHandler>()
            where TEvent : IntegrationEvent
            where TEventHandler : IIntegrationEventHandler<TEvent>
        {
            var eventName = _subsManager.GetEventKey<TEvent>();

            _logger.LogInformation("Unsubscribing from event {EventName}", eventName);

            _subsManager.RemoveSubscription<TEvent, TEventHandler>();
        }

        public void Dispose()
        {
            _consumerChannel?.Dispose();

            _subsManager.Clear();
        }
    }
}
