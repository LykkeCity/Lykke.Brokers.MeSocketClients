using System.Net;
using System.Threading.Tasks;
using Common.Log;
using Core;
using Core.Services;
using Core.Tools;

namespace Services
{
    public class OrderBookReader : IOrderBookReader
    {
        private readonly BaseSettings _settings;
        private readonly IOrderBooksHandler _orderBooksHandler;
        private readonly ILog _log;

        private readonly SimpleClientTcpSocket _clientTcpSocket;

        public OrderBookReader(BaseSettings settings,
            IOrderBooksHandler orderBooksHandler,
            ITcpDeserializer tcpDeserializer,
            ILog log)
        {
            _settings = settings;
            _orderBooksHandler = orderBooksHandler;
            _log = log;
            _clientTcpSocket = new SimpleClientTcpSocket("OrderBookSocket",
                new IPEndPoint(IPAddress.Parse(_settings.MatchingEngine.IpEndpoint.InternalHost),
                    _settings.MatchingEngine.ServerOrderBookPort), 2, log, tcpDeserializer, HandleData, 4, 4);
        }

        public async Task StartRead()
        {
            await _clientTcpSocket.Start();
        }

        private async Task HandleData(object data)
        {
            var result = data as MeOrderBookModel;

            if (result != null)
            {
                await _orderBooksHandler.HandleOrderBook(result.ConvertToDomainModel());
            }
        }
    }
}
