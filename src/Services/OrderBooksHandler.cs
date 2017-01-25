using System.Threading.Tasks;
using Common;
using Core;
using Core.Domain;
using Core.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace Services
{
    public class OrderBooksHandler : IOrderBooksHandler
    {
        private readonly IDistributedCache _cache;
        private readonly BaseSettings _settings;

        public OrderBooksHandler(IDistributedCache cache, BaseSettings settings)
        {
            _cache = cache;
            _settings = settings;
        }

        public async Task HandleOrderBook(IOrderBook orderBook)
        {
            await _cache.SetStringAsync(_settings.CacheSettings.GetOrderBookKey(orderBook.AssetPair, orderBook.IsBuy), orderBook.ToJson());
        }
    }
}
