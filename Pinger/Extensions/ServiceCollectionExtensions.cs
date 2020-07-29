using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pinger.IntegrationEvents.EventHandling;
using RabbitMQ.Client;
using RabbitMQ.Wrapper;
using RabbitMQ.Wrapper.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinger.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddIntegrationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddLogging(configure => configure.AddConsole());

            services.AddSingleton<IRabbitMqPersistentConnection>(s =>
            {
                var logger = s.GetRequiredService<ILogger<DefaultRabbitMqPersistentConnection>>();

                var factory = new ConnectionFactory
                {
                    HostName = configuration["EventBusConnection"],
                    DispatchConsumersAsync = true
                };

                if (!string.IsNullOrEmpty(configuration["EventBusUserName"]))
                {
                    factory.UserName = configuration["EventBusUserName"];
                }

                if (!string.IsNullOrEmpty(configuration["EventBusPassword"]))
                {
                    factory.Password = configuration["EventBusPassword"];
                }

                var retryCount = 5;
                if (!string.IsNullOrEmpty(configuration["EventBusRetryCount"]))
                {
                    retryCount = int.Parse(configuration["EventBusRetryCount"]);
                }

                return new DefaultRabbitMqPersistentConnection(factory, logger, retryCount);
            });

            return services;
        }

        public static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IEventBus, EventBusRabbitMq>(s =>
            {
                var serviceBusConnection = s.GetRequiredService<IRabbitMqPersistentConnection>();
                var iLifetimeScope = s.GetRequiredService<ILifetimeScope>();
                var logger = s.GetRequiredService<ILogger<EventBusRabbitMq>>();
                var subscriptionsManager = s.GetRequiredService<IEventBusSubscriptionsManager>();

                var eventBus = new EventBusRabbitMq(serviceBusConnection, logger, subscriptionsManager, iLifetimeScope, "ping_queue", 5);

                return eventBus;
            });

            services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();

            services.AddTransient<PongerBouncedBallEventHandler>();

            return services;
        }
    }
}
