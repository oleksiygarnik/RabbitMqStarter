using Newtonsoft.Json;
using System;

namespace RabbitMQ.Wrapper.Events
{
    public class IntegrationEvent
    {
        [JsonProperty]
        public Guid Id { get; private set; }

        [JsonProperty]
        public DateTime CreatedAt { get; set; }

        public IntegrationEvent() : this(Guid.NewGuid(), DateTime.Now)
        {

        }
        public IntegrationEvent(Guid id, DateTime createdAt)
        {
            Id = id;
            CreatedAt = createdAt;
        }
    }
}
