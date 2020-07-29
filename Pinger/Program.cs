using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pinger.Extensions;
using Pinger.IntegrationEvents.EventHandling;
using Pinger.IntegrationEvents.Events;
using RabbitMQ.Client;
using RabbitMQ.Wrapper;
using RabbitMQ.Wrapper.Abstractions;
using RabbitMQ.Wrapper.Events;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Pinger
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


            var eventBus = serviceProvider.GetService<IEventBus>();

            eventBus.ListenQueue<PongerBouncedBallEvent, PongerBouncedBallEventHandler>();

            Console.ReadLine();
        }
    }
}
