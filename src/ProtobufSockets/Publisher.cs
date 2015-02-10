using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ProtobufSockets.Internal;
using ProtobufSockets.Stats;

namespace ProtobufSockets
{
    public class Publisher : IDisposable
    {
        const LogTag Tag = LogTag.Publisher;

        readonly TcpListener _listener;
        readonly PublisherSubscriptionStore _store;
        readonly ProtoSerialiser _serialiser = new ProtoSerialiser();
        private Timer _timer;

        public Publisher() : this(new IPEndPoint(IPAddress.Loopback, 0))
        {
        }

        public Publisher(IPEndPoint ipEndPoint)
        {
            _listener = new TcpListener(ipEndPoint);
            _listener.Start();
            _listener.BeginAcceptTcpClient(ClientAccept, null);
            _store = new PublisherSubscriptionStore();
            _timer = new Timer(Publish, new Beat(), 10*1000, 5*1000);
        }

        public IPEndPoint EndPoint
        {
            get
            {
                return (IPEndPoint)_listener.Server.LocalEndPoint;
            }
        }

        public void Publish<T>(T message)
        {
            Publish(null, message);
        }

        public void Publish<T>(string topic, T message)
        {
            if (topic != null)
                topic = topic.TrimEnd('*', '.');

            foreach (var client in _store.Subscriptions)
            {
                Log.Debug(Tag, "Publishing message..");

                if (!Topic.Match(client.Topic, topic)) continue;

                client.Send(topic, typeof(T), message);
            }
        }

        public PublisherStats GetStats(bool withSystemStats = false)
        {
            var clients = _store.Subscriptions
                .Select(client => new PublisherClientStats(
                    client.Name,
                    client.MessageLoss,
                    client.Backlog,
                    client.EndPoint,
                    client.Topic,
                    client.Type,
                    client.MessageCount))
                .ToList();

            var statsBuilder = new SystemStatsBuilder();
            if (withSystemStats)
                statsBuilder.ReadInFromCurrentProcess();

            return new PublisherStats(clients, statsBuilder.Build());
        }

        public void Dispose()
        {
            Log.Info(Tag, "Disposing.");

            _timer.Dispose();

            foreach (var client in _store.Subscriptions)
            {
                Log.Debug(Tag, "Closing client " + (client.Name ?? "<null>") +" ["+ (client.EndPoint ?? "<null>") + "]");
                client.Close();
            }

            _listener.Stop();
        }

        void ClientAccept(IAsyncResult ar)
        {
            Log.Debug(Tag, "Accepting client connections.");

            TcpClient tcpClient;
            try
            {
                tcpClient = _listener.EndAcceptTcpClient(ar);
                Log.Info(Tag, "A client connection is accepted.");
            }
            catch (NullReferenceException)
            {
                return; // Listener already stopped
            }
            catch (InvalidOperationException)
            {
                return; // Listener already stopped
            }

            _listener.BeginAcceptTcpClient(ClientAccept, null);

            Socket socket = null;
            var success = false;
            try
            {
                tcpClient.NoDelay = true;
                tcpClient.LingerState.Enabled = true;
                tcpClient.LingerState.LingerTime = 0;

                NetworkStream networkStream = tcpClient.GetStream();

                socket = tcpClient.Client;

                // handshake
                var header = _serialiser.Deserialize<Header>(networkStream);

                Log.Info(Tag, "Client connected with topic " + (header.Topic ?? "<null>") + " and name " + (header.Name ?? "<null>"));

                var client = new PublisherClient(tcpClient, networkStream, header, _store);
                
                _store.Add(socket, client);

                // send ack
                _serialiser.Serialise(networkStream, "OK");

                success = true;

                Log.Debug(Tag, "Successfully connected " + socket.RemoteEndPoint);
            }
            catch (ProtoSerialiserException)
            {
                Log.Error(Tag, "ProtoSerialiserException");
            }
            catch (SocketException e)
            {
                Log.Error(Tag, "SocketException: " + e.Message + " [SocketErrorCode:" + e.SocketErrorCode + "]");
            }
            catch (IOException e)
            {
                Log.Error(Tag, "IOException: " + e.Message);
            }

            if (success) return;

            Log.Debug(Tag, "Connection unsuccessful.");

            _store.Remove(socket);
            tcpClient.Close();
        }
    }
}
