using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Log;
using Core;
using Core.Services;
using RestSharp;
using Common;
using Core.Domain;

namespace Services
{
    public class OrderBookInitializer : IOrderBookInitializer
    {
        private readonly IOrderBooksHandler _orderBooksHandler;
        private readonly IRestClient _restClient;
        private readonly ILog _log;

        public OrderBookInitializer(IOrderBooksHandler orderBooksHandler, IRestClient restClient, BaseSettings settings, ILog log)
        {
            _orderBooksHandler = orderBooksHandler;
            _restClient = restClient;
            _log = log;
            _restClient.BaseUrl = settings.MatchingEngine.GetOrderBookInitUri();
        }

        public async Task InitOrderBooks()
        {
            var request = new RestRequest(Method.GET);

            var t = new TaskCompletionSource<IRestResponse>();
            _restClient.ExecuteAsync(request, resp => { t.SetResult(resp); });
            var response = await t.Task;

            if (response.ResponseStatus == ResponseStatus.Completed && response.StatusCode == HttpStatusCode.OK)
            {
                var orderBooks = response.Content.DeserializeJson<OrderBook[]>();
                if (orderBooks != null && orderBooks.Any())
                {
                    foreach (var orderBook in orderBooks)
                    {
                        await _orderBooksHandler.HandleOrderBook(orderBook);
                    }
                }
                else
                {
                    await _log.WriteWarningAsync("OrderBookInitializer", "InitOrderBooks", "", "No orderbooks on init");
                }

                return;
            }

            var exception = response.ErrorException ?? new Exception(response.Content);
            await _log.WriteErrorAsync("OrderBookInitializer", "InitOrderBooks", "", exception);

            throw exception;
        }
    }
}
