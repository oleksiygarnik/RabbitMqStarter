using RabbitMQ.Wrapper.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Wrapper.Abstractions
{
    public interface IIntegrationEventHandler
    {
    }
    
    public interface IIntegrationEventHandler<in TEvent> : IIntegrationEventHandler
        where TEvent : IntegrationEvent
    {
        Task Handle(TEvent @event);
    }
}
