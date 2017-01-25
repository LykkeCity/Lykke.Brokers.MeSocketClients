using Autofac;
using Core.Services;

namespace Services
{
    public static class ServicesBinder
    {
        public static void BindServices(this ContainerBuilder ioc)
        {
            ioc.RegisterType<OrderBookReader>().As<IOrderBookReader>();
            ioc.RegisterType<OrderBooksHandler>().As<IOrderBooksHandler>();
            ioc.RegisterType<MeTcpDeserializer>().As<ITcpDeserializer>();
        }
    }
}
