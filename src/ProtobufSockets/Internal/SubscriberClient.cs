using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace ProtobufSockets.Internal
{
    class SubscriberClient
    {
        const LogTag Tag = LogTag.SubscriberClient;

        long _messageCount;
        int _disposed;

        readonly IPEndPoint _endPoint;
        readonly TcpClient _tcpClient;
        readonly Thread _consumerThread;
        readonly NetworkStream _networkStream;
        readonly Type _type;
        readonly ProtoSerialiser _serialiser;
        readonly Action<object> _action;
        readonly Action<IPEndPoint> _disconnected;

        internal static SubscriberClient Connect(IPEndPoint endPoint, string name, string topic, Type type, Action<object> action, Action<IPEndPoint> disconnected)
        {
            Log.Debug(Tag, "Connecting to " + endPoint);
            try
            {
                var serialiser = new ProtoSerialiser();
                var tcpClient = new TcpClient {NoDelay = true, LingerState = {Enabled = true, LingerTime = 0}};
                tcpClient.Connect(endPoint);
                var networkStream = tcpClient.GetStream();

                serialiser.Serialise(networkStream, new Header {Topic = topic, Type = type.FullName, Name = name});
                var ack = serialiser.Deserialize<string>(networkStream);

                if (ack != "OK")
                {
                    return null;
                }

                Log.Debug(Tag, "Subscribing started.");

                return new SubscriberClient(endPoint, type, action, disconnected, tcpClient, networkStream, serialiser);
            }
            catch (InvalidOperationException)
            {
                Log.Debug(Tag, "Connect: InvalidOperationException");
            }
            catch (SocketException e)
            {
                Log.Debug(Tag, "Connect: SocketException: " + e.Message + " [SocketErrorCode:" + e.SocketErrorCode + "]");
            }
            catch (ProtoSerialiserException)
            {
                Log.Error(Tag, "Connect: ProtoSerialiserException");
            }
            catch (IOException e)
            {
                Log.Debug(Tag, "Connect: IOException: " + e.Message);
            }

            Log.Debug(Tag, "Error connecting to " + endPoint);

            return null;
        }

        SubscriberClient(IPEndPoint endPoint, Type type, Action<object> action, Action<IPEndPoint> disconnected, TcpClient tcpClient, NetworkStream networkStream, ProtoSerialiser serialiser)
        {
            _endPoint = endPoint;
            _type = type;
            _action = action;
            _disconnected = disconnected;
            _tcpClient = tcpClient;
            _networkStream = networkStream;
            _serialiser = serialiser;

            _consumerThread = new Thread(Consume) { IsBackground = true };
            _consumerThread.Start();
        }

        internal long GetMessageCount()
        {
            return Interlocked.CompareExchange(ref _messageCount, 0, 0);
        }

        internal void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1) return;

            Interlocked.Exchange(ref _disposed, 1);
            _tcpClient.Close();
            _disconnected(_endPoint);
        }

        void Consume()
        {
            var typeName = _type.FullName;

            Log.Info(Tag, "Consumer started [" + Thread.CurrentThread.ManagedThreadId + "]");

            while (Interlocked.CompareExchange(ref _disposed, 0, 0) == 0)
            {
                try
                {
                    var header = _serialiser.Deserialize<Header>(_networkStream);

                    Log.Debug(Tag,
                        "Received header [name=" + (header.Name ?? "<null>") + " type=" + (header.Type ?? "<null>") +
                        " topic=" + (header.Topic ?? "<null>") + "]");

                    if (header.Type != typeName)
                    {
                        Log.Debug(Tag, "Ignoring unmatched type. (Subscribed with wrong type?)");
                        _serialiser.Chew(_networkStream);
                        continue;
                    }

                    var message = _serialiser.Deserialize(_networkStream, _type);

                    Log.Debug(Tag, "Received message.");

                    Interlocked.Increment(ref _messageCount);

                    _action(message);
                }
                catch (SocketException e)
                {
                    Log.Debug(Tag, "Consume: SocketException: " + e.Message + " [SocketErrorCode:" + e.SocketErrorCode + "]");
                    break;
                }
                catch (IOException e)
                {
                    Log.Debug(Tag, "Consume: IOException: " + e.Message);
                    break;
                }
                catch (ProtoSerialiserException)
                {
                    Log.Error(Tag, "Consume: ProtoSerialiserException");
                    break;
                }
                catch (ObjectDisposedException)
                {
                    Log.Debug(Tag, "Connect: ObjectDisposedException");
                    break;
                }
            }

            Dispose();

            Log.Info(Tag, "Consumer exiting [" + Thread.CurrentThread.ManagedThreadId + "]");
        }
    }
}

