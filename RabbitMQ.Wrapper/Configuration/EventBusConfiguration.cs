using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace RabbitMQ.Wrapper.Configuration
{
    public class EventBusConfiguration
    {
        public string EventBusConnection { get; set; }
        public string EventBusUserName { get; set; }
        public string EventBusPassword { get; set; }
        public int EventBusRetryCount { get; set; } = 5;
        public EventBusType EventBusType { get; set; }
        public string SubscriptionClientName { get; set; }
    }

    public enum EventBusType
    {
        [Description("RabbitMq")]
        RabbitMq = 1,

        [Description("Azure")]
        Azure
    }
}
