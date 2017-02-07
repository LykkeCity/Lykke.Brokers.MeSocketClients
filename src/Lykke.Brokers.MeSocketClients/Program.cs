using System;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Core;
using Core.Services;
using Lykke.Brokers.MeSocketClients.Binders;
using Microsoft.Extensions.Configuration;
using Repositories;

namespace Lykke.Brokers.MeSocketClients
{
    public class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();

            var settings = SettingsReader.ReadGeneralSettings<BaseSettings>(configuration.GetConnectionString("Azure"));

            var container = new AzureBinder().Bind(settings).Build();

            StartAsync(container).Wait();
        }

        static async Task StartAsync(IContainer container)
        {
            var log = container.Resolve<ILog>();

            await log.WriteInfoAsync("MeSocketClients", "Main", "", "Starting...");
            try
            {
                var orderBookReader = container.Resolve<IOrderBookReader>();
                orderBookReader.StartRead();
            }
            catch (Exception ex)
            {
                await log.WriteErrorAsync("MeSocketClients", "Main", "", ex);
                throw;
            }
        }
    }
}
