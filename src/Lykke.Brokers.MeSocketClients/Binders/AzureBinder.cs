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
            var log = new LogToTableAndConsole(logToTable, new LogToConsole());
            var ioc = new ContainerBuilder();
            InitContainer(ioc, settings, log);
            return ioc;
        }

        private void InitContainer(ContainerBuilder ioc, BaseSettings settings, ILog log)
        {
            log.WriteInfoAsync("MeSocketClients", "App start", null, $"BaseSettings : {settings.ToJson()}").Wait();

            ioc.RegisterInstance(log);
            ioc.RegisterInstance(settings);

            ioc.BindServices();
            ioc.BindAzure(settings);

            var redis = new RedisCache(new RedisCacheOptions
            {
                Configuration = settings.CacheSettings.RedisConfiguration,
                InstanceName = settings.CacheSettings.FinanceDataCacheInstance
            });

            ioc.RegisterInstance(redis).As<IDistributedCache>();

            ioc.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        }
    }
}
