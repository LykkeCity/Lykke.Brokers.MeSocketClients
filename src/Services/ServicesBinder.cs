using Autofac;
using Core;
using Core.Services;
using RestSharp;
using RabbitMqSettings = Lykke.RabbitMqBroker.RabbitMqSettings;

namespace Services
{
    public static class ServicesBinder
    {
        public static void BindServices(this ContainerBuilder ioc, BaseSettings settings)
        {
            var rabbitSettings = new RabbitMqSettings
            {
                ConnectionString = settings.MatchingEngine.RabbitMq.GetConnectionString(),
                QueueName = settings.MatchingEngine.RabbitMq.ExchangeOrderbook
            };


            ioc.Register(x => new RestClient()).As<IRestClient>();
            ioc.RegisterType<OrderBookInitializer>().As<IOrderBookInitializer>();

            ioc.RegisterInstance(rabbitSettings);
            ioc.RegisterType<OrderBookReader>().As<IOrderBookReader>().SingleInstance();
            ioc.RegisterType<OrderBooksHandler>().As<IOrderBooksHandler>().SingleInstance();
        }
    }
}
