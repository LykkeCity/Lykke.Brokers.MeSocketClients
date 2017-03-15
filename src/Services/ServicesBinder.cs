using Autofac;
using Core;
using Core.Services;
using RestSharp;
using StackExchange.Redis;
using RabbitMqSettings = Lykke.RabbitMqBroker.RabbitMqSettings;

namespace Services
{
    public static class ServicesBinder
    {
        public static void BindServices(this ContainerBuilder ioc, BaseSettings settings)
        {
            var redis = ConnectionMultiplexer.Connect(settings.CacheSettings.RedisInternalHost);

            ioc.RegisterInstance(redis).SingleInstance();
            ioc.Register(
                c =>
                    c.Resolve<ConnectionMultiplexer>()
                        .GetServer(settings.CacheSettings.RedisInternalHost, settings.CacheSettings.RedisPort));

            ioc.Register(
                c =>
                    c.Resolve<ConnectionMultiplexer>()
                        .GetDatabase());

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
