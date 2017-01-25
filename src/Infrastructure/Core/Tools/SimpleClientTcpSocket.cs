using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Common.Log;
using Core.Services;

namespace Core.Tools
{

    internal class SimpleTcpSocketConnection
    {
        private readonly SimpleClientTcpSocket _socket;
        private readonly TcpClient _tcpClient;
        private readonly ITcpDeserializer _tcpDeserializer;
        private readonly ILog _log;
        private readonly Func<object, Task> _dataHandler;
        public int Id { get;}
        public bool Disconnected { get; private set; }
        private readonly object _lockObject = new object();
        internal DateTime LastReadData = DateTime.UtcNow;

        public SimpleTcpSocketConnection(SimpleClientTcpSocket socket, TcpClient tcpClient,
            ITcpDeserializer tcpDeserializer, int id, ILog log, Func<object, Task> dataHandler)
        {
            _socket = socket;
            _tcpClient = tcpClient;
            _tcpDeserializer = tcpDeserializer;
            _log = log;
            _dataHandler = dataHandler;
            Id = id;
        }

        private async Task PingProcess()
        {
            var pingPacket = _tcpDeserializer.CreatePingPacket();
            if (pingPacket != null)
                while (!Disconnected)
                {
                    if ((DateTime.UtcNow - LastReadData).TotalSeconds > _socket.PingTimeOut)
                    {
                        await Send(pingPacket);


                        await Task.Delay(_socket .PacketDeliveryTimeOut* 1000);

                        var now = DateTime.UtcNow;
                        var seconds = (now - LastReadData).TotalSeconds;

                        if (seconds > _socket.PingTimeOut)
                        {
                            await _log.WriteInfoAsync("SimpleClientTcpSocket", "PingProcess", "",
                                $"{_socket.SocketName}: Ping detected invalid connection:{Id}. Disconnect");
                            Disconnect();
                            break;
                        }

                    }
                    await Task.Delay(_socket.PingTimeOut);
                }
        }


        private async Task ReadData()
        {
            var stream = _tcpClient.GetStream();

            while (!Disconnected)
            {
                LastReadData = DateTime.UtcNow;

                var msg = await _tcpDeserializer.Deserialize(stream);

                await _dataHandler(msg.Item1);
            }
        }



        public async Task ReadThread()
        {
            try
            {
                await Task.WhenAll(
                    ReadData(),
                    PingProcess()
                    );

            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            try
            {
                lock (_lockObject)
                {
                    if (Disconnected)
                        return;

                    Disconnected = true;
                }

                _tcpClient.Dispose();
            }
            catch (Exception exception)
            {
                _log.WriteErrorAsync("SimpleClientTcpSocket", "Disconnect", "", exception).Wait();
            }

        }

        public Task Send(byte[] data)
        {
            if (Disconnected)
                return Task.FromResult(0);

            try
            {
                var stream = _tcpClient.GetStream();
                return stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception)
            {
                Disconnect();
            }

            return Task.FromResult(0);
        }
    }


    public class SimpleClientTcpSocket
    {
        internal readonly string SocketName;
        private readonly IPEndPoint _ipEndPoint;
        private readonly int _reconnectTimeOut;
        private readonly ILog _log;
        private readonly ITcpDeserializer _tcpDeserializer;
        private readonly Func<object, Task> _dataHandler;
        internal readonly int PingTimeOut;
        internal readonly int PacketDeliveryTimeOut;

        private bool _working;

        private int _socketId;


        public SimpleClientTcpSocket(string socketName, IPEndPoint ipEndPoint, int reconnectTimeOut, 
            ILog log, ITcpDeserializer tcpDeserializer, Func<object, Task> dataHandler, int pingTimeOut = 2, int packetDeliveryTimeOut=2)
        {
            SocketName = socketName;
            _ipEndPoint = ipEndPoint;
            _reconnectTimeOut = reconnectTimeOut;
            _log = log;
            _tcpDeserializer = tcpDeserializer;
            _dataHandler = dataHandler;
            PingTimeOut = pingTimeOut;
            PacketDeliveryTimeOut = packetDeliveryTimeOut;
        }

        private async Task<SimpleTcpSocketConnection> Connect()
        {
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(_ipEndPoint.Address, _ipEndPoint.Port);

            return new SimpleTcpSocketConnection(this, tcpClient, _tcpDeserializer, _socketId++, _log, _dataHandler);
        }


        private async Task SocketThread()
        {
            while (_working)
            {
                try
                {
                    var connection = await Connect();
                    await _log.WriteInfoAsync("SimpleClientTcpSocket", "SocketThread", "", $"Connected to server:{_ipEndPoint}. Id:{connection.Id}");

                    await connection.ReadThread();
                }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync("SimpleClientTcpSocket", "SocketThread", "", ex);
                }

                await Task.Delay(_reconnectTimeOut);

            }

        }

        public Task Start()
        {
            if (_working)
                throw new Exception("Client socket has already started");
            _working = true;

            return SocketThread();
        }
    }

}
