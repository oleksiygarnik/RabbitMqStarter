using Newtonsoft.Json;
using RabbitMQ.Wrapper;
using RabbitMQ.Wrapper.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ponger.IntegrationEvents.Events
{
    public class PingerBouncedBallEvent : IntegrationEvent
    {
        [JsonProperty]
        public string From { get; set; }

        [JsonProperty]
        public string To { get; set; }

        [JsonProperty]
        public string Message { get; set; }

        public PingerBouncedBallEvent()
        {

        }
        public PingerBouncedBallEvent(string from, string to, string message) : base()
        {
            if (string.IsNullOrEmpty(from))
                throw new ArgumentException(nameof(from));

            if (string.IsNullOrEmpty(to))
                throw new ArgumentException(nameof(to));

            if (string.IsNullOrEmpty(message))
                throw new ArgumentException(nameof(message));

            From = from;
            To = to;
            Message = message;
        }
    }
}
