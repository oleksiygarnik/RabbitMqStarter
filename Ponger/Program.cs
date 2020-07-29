using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ponger.Extensions;
using Ponger.IntegrationEvents.EventHandling;
using Ponger.IntegrationEvents.Events;
using RabbitMQ.Client;
using RabbitMQ.Wrapper;
using RabbitMQ.Wrapper.Abstractions;
using System;
using System.IO;
using System.Linq;

namespace Ponger
{
    class Program
    {

        static void Main(string[] args)
        {
            //Settings for IoC
            var configurationBuilder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json");

            var configuration = configurationBuilder.Build();

            var services = new ServiceCollection()
                .AddIntegrationServices(configuration)
                .AddEventBus(configuration);


            var builder = new ContainerBuilder();
            builder.Populate(services);
            var serviceProvider = new AutofacServiceProvider(builder.Build());

            services.AddLogging(configure => configure.AddConsole());

            var eventBus = serviceProvider.GetService<IEventBus>();

            eventBus.ListenQueue<PingerBouncedBallEvent, PingerBouncedBallEventHandler>();

            var @event = new PongerBouncedBallEvent
            {
                From = "Ponger",
                To = "Pinger",
                Message = "pong"
            };

            eventBus.SendMessageToQueue(@event);

            Console.ReadLine();
        }
    }
}
