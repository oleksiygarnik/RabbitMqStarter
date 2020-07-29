using Microsoft.Extensions.Logging;
using Pinger.IntegrationEvents.Events;
using RabbitMQ.Wrapper;
using RabbitMQ.Wrapper.Abstractions;
using System;
using System.Threading.Tasks;

namespace Pinger.IntegrationEvents.EventHandling
{
    public class PongerBouncedBallEventHandler : IIntegrationEventHandler<PongerBouncedBallEvent>
    {
        private readonly ILogger<PongerBouncedBallEventHandler> _logger;
        private readonly IEventBus _eventBus;

        public PongerBouncedBallEventHandler(ILogger<PongerBouncedBallEventHandler> logger, IEventBus eventBus)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }
        public async Task Handle(PongerBouncedBallEvent @event)
        {
            if (@event is null)
                throw new ArgumentNullException(nameof(@event));

            _logger.LogInformation($"CreatedAt: {@event.CreatedAt}, ball was bounced from {@event.From} to {@event.To} with message {@event.Message}");

            await Task.Delay(2500);

            var from = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
            var to = "Ponger";

            var newEvent = new PingerBouncedBallEvent(from, to, "ping");

            _eventBus.SendMessageToQueue(newEvent);
        }
    }
}
