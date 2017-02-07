using Autofac;
using Autofac.Features.ResolveAnything;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using Repositories;
using Repositories.Log;
using Services;

namespace Lykke.Brokers.MeSocketClients.Binders
{
    public class AzureBinder
    {
        public const string DefaultConnectionString = "UseDevelopmentStorage=true";

        public ContainerBuilder Bind(BaseSettings settings)
        {
            var logToTable = new LogToTable(new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, "LogMeSocketClients", null));
            var ioc = new ContainerBuilder();
            InitContainer(ioc, settings, logToTable);
            return ioc;
        }

        private void InitContainer(ContainerBuilder ioc, BaseSettings settings, ILog log)
        {
            log.WriteInfoAsync("MeSocketClients", "App start", null, $"BaseSettings : {settings.ToJson()}").Wait();

            ioc.RegisterInstance(log);
            ioc.RegisterInstance(settings);

            ioc.BindServices(settings);
            ioc.BindAzure(settings);

            var redis = new RedisCache(new RedisCacheOptions
            {
                Configuration = $"{settings.CacheSettings.RedisInternalHost}:{settings.CacheSettings.RedisPort}",
                InstanceName = settings.CacheSettings.FinanceDataCacheInstance
            });

            ioc.RegisterInstance(redis).As<IDistributedCache>();

            ioc.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        }
    }
}
