using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using ProtoBuf;
using ProtobufSockets.Internal;
using ProtobufSockets.Stats;

namespace ProtobufSockets
{
    public class Publisher : IDisposable
    {
        private const LogTag Tag = LogTag.Publisher;

        private readonly TcpListener _listener;
        private readonly PublisherSubscriptionStore _store;

        public Publisher() : this(new IPEndPoint(IPAddress.Loopback, 0))
        {
        }

        public Publisher(IPEndPoint ipEndPoint)
        {
            _listener = new TcpListener(ipEndPoint);
            _listener.Start();
            _listener.BeginAcceptTcpClient(ClientAccept, null);
            _store = new PublisherSubscriptionStore();
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
                Log.Debug(Tag, "publishing message..");

                if (!Topic.Match(client.Topic, topic)) continue;
                
                client.Send(topic, typeof(T), message);
            }
        }

        public PublisherStats GetStats()
        {
            var clients = _store.Subscriptions
                .Select(client => new PublisherClientStats(
                    client.Name,
                    client.MessageLoss,
                    client.Backlog,
                    client.EndPoint,
                    client.Topic,
                    client.MessageCount))
                .ToList();

            return new PublisherStats(clients);
        }

        public void Dispose()
        {
            Log.Info(Tag, "disposing..");

            foreach (var client in _store.Subscriptions)
            {
                Log.Debug(Tag, "closing client..");
                client.Close();
            }

            _listener.Stop();
        }

        private void ClientAccept(IAsyncResult ar)
        {
            TcpClient tcpClient;

			try
            {
                tcpClient = _listener.EndAcceptTcpClient(ar);
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
            try
            {
                tcpClient.NoDelay = true;
                tcpClient.LingerState.Enabled = true;
                tcpClient.LingerState.LingerTime = 0;

                Log.Info(Tag, "client connected..");

                NetworkStream networkStream = tcpClient.GetStream();

                var client = new PublisherClient(tcpClient, networkStream, _store);

                socket = tcpClient.Client;
                _store.Add(socket, client);

                var header = Serializer.DeserializeWithLengthPrefix<Header>(networkStream, PrefixStyle.Base128);
                Log.Info(Tag, "client topic is.. " + header.Topic);

                Serializer.SerializeWithLengthPrefix(networkStream, "OK", PrefixStyle.Base128);

                client.SetServerAck(header);
            }
            catch (Exception e)
            {
                Log.Error(Tag, "UNEXPECTED_ERROR_PUB1: {0} : {1}", e.GetType(), e.Message);
                _store.Remove(socket);
                tcpClient.Close();
            }
        }
    }
}