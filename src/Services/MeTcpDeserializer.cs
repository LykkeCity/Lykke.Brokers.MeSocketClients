using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProtoBuf;
using Common;
using Common.Log;
using Core.Domain;
using Core.Services;

namespace Services
{
    public class MeTcpDeserializer : ITcpDeserializer
    {
        private readonly ILog _log;

        private static readonly Dictionary<MeDataType, Type> Types = new Dictionary<MeDataType, Type>
        {
            [MeDataType.Ping] = typeof(MePingModel),
            [MeDataType.OrderBook] = typeof(MeOrderBookModel)
        };

        private static readonly Dictionary<Type, MeDataType> TypesReverse = new Dictionary<Type, MeDataType>();

        static MeTcpDeserializer()
        {
            foreach (var tp in Types)
                TypesReverse.Add(tp.Value, tp.Key);
        }

        public MeTcpDeserializer(ILog log)
        {
            _log = log;
        }

        public async Task<Tuple<object, int>> Deserialize(Stream stream)
        {
            try
            {
                const int headerSize = 5;

                var dataType = (MeDataType)await stream.ReadByteFromSocket();

                var datalen = await stream.ReadIntFromSocket();

                var data = await stream.ReadFromSocket(datalen);
                var memStream = new MemoryStream(data) { Position = 0 };

                if (Types.ContainsKey(dataType))
                {
                    if (dataType == MeDataType.Ping)
                        return null;

                    var result = Serializer.NonGeneric.Deserialize(Types[dataType], memStream);
                    return new Tuple<object, int>(result, headerSize + datalen);
                }

                await _log.WriteWarningAsync("MeTcpDeserializer", "Deserialize", dataType.ToString(), "Unkonw data type");
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync("MeTcpDeserializer", "Deserialize", "", ex);
            }
            return null;
        }

        public byte[] Serialize(object data)
        {
            var type = TypesReverse[data.GetType()];

            var memStream = new MemoryStream();
            Serializer.Serialize(memStream, data);
            var outData = memStream.ToArray();

            var outStream = new MemoryStream();
            outStream.WriteByte((byte)type);
            outStream.WriteInt(outData.Length);
            outStream.Write(outData, 0, outData.Length);

            return outStream.ToArray();
        }

        public byte[] CreatePingPacket()
        {
            return new []{(byte)MeDataType.Ping};
        }
    }

    public enum MeDataType
    {
        Unknown,
        Ping,
        OrderBook = 40
    }

    public class MePingModel
    {
        public static readonly MePingModel Instance = new MePingModel();
    }

    [ProtoContract]
    public class MeOrderBookModel
    {
        [ProtoMember(1, IsRequired = true)]
        public string Asset { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public bool IsBuy { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public long Timestamp { get; set; }

        [ProtoMember(4)]
        public MeOrderBookLevelModel[] Levels { get; set; }
    }

    [ProtoContract]
    public class MeOrderBookLevelModel
    {
        [ProtoMember(1, IsRequired = true)]
        public string Price { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public string Volume { get; set; }
    }

    public static class Ext
    {
        public static IOrderBook ConvertToDomainModel(this MeOrderBookModel meModel)
        {
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime timestamp = start.AddMilliseconds(meModel.Timestamp);

            return new OrderBook
            {
                AssetPair = meModel.Asset,
                IsBuy = meModel.IsBuy,
                Prices = meModel.Levels?.Select(x => new VolumePrice
                {
                    Price = x.Price.ParseAnyDouble(),
                    Volume = x.Volume.ParseAnyDouble()
                }).ToList(),
                Timestamp = timestamp
            };
        }
    }
}
