using RabbitMQ.Wrapper.Events;
using System.Collections.Generic;

namespace RabbitMQ.Wrapper.Abstractions
{
    public interface IEventBus
    {
        // Publish
        void SendMessageToQueue(IntegrationEvent @event);

        // Publish
        void SendMessagesToQueue(IEnumerable<IntegrationEvent> @events);

        // Subscribe
        void ListenQueue<TEvent, TEventHandler>()
            where TEvent : IntegrationEvent
            where TEventHandler : IIntegrationEventHandler<TEvent>;

        void Unsubscribe<TEvent, TEventHandler>()
           where TEvent : IntegrationEvent
           where TEventHandler : IIntegrationEventHandler<TEvent>;

    }
}
