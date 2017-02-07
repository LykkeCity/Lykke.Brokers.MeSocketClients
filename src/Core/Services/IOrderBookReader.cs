using System.Threading.Tasks;
using Core.Domain;

namespace Core.Services
{
    public interface IOrderBooksHandler
    {
        Task HandleOrderBook(IOrderBook orderBook);
    }
    
    public interface IOrderBookReader
    {
        void StartRead();
    }
}
