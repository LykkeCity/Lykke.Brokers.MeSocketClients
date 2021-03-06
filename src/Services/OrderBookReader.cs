﻿using System.Threading.Tasks;
using Common.Log;
using Core.Domain;
using Core.Services;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Services.Tools;

namespace Services
{
    public class OrderBookReader : IOrderBookReader
    {
        private readonly IOrderBooksHandler _orderBooksHandler;
        private readonly RabbitMqSubscriber<OrderBook> _connector;

        public OrderBookReader(RabbitMqSettings settings,
            IOrderBooksHandler orderBooksHandler,
            ILog log)
        {
            _orderBooksHandler = orderBooksHandler;

            _connector =
                new RabbitMqSubscriber<OrderBook>(settings)
                    .SetMessageDeserializer(new OrderBookDeserializer())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .Subscribe(HandleData)
                    .SetLogger(log);
        }

        public void StartRead()
        {
            _connector.Start();
        }

        private async Task HandleData(IOrderBook orderBook)
        {
            if (orderBook != null)
            {
                await _orderBooksHandler.HandleOrderBook(orderBook);
            }
        }
    }
}
