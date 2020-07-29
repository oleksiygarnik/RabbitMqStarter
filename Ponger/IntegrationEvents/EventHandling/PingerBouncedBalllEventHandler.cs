using Microsoft.Extensions.Logging;
using Ponger.IntegrationEvents.Events;
using RabbitMQ.Wrapper;
using RabbitMQ.Wrapper.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ponger.IntegrationEvents.EventHandling
{
    public class PingerBouncedBallEventHandler : IIntegrationEventHandler<PingerBouncedBallEvent>
    {
        private readonly ILogger<PingerBouncedBallEventHandler> _logger;
        private readonly IEventBus _eventBus;

        public PingerBouncedBallEventHandler(ILogger<PingerBouncedBallEventHandler> logger, IEventBus eventBus)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }
        public async Task Handle(PingerBouncedBallEvent @event)
        {
            if (@event is null)
                throw new ArgumentNullException(nameof(@event));

            _logger.LogInformation($"CreatedAt: {@event.CreatedAt}, ball was bounced from {@event.From} to {@event.To} with message {@event.Message}");

            await Task.Delay(2500);

            var from = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
            var to = "Pinger";

            var newEvent = new PongerBouncedBallEvent(from, to, "pong");

            _eventBus.SendMessageToQueue(newEvent);
        }
    }
}
