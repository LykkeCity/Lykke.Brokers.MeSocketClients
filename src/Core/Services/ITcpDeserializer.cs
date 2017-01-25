using System;
using System.IO;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface ITcpDeserializer
    {
        Task<Tuple<object, int>> Deserialize(Stream stream);
        byte[] Serialize(object data);
        byte[] CreatePingPacket();
    }
}
