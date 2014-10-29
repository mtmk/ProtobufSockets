using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace ProtobufSockets.Internal
{
    internal class PublisherClient
    {
        private const LogTag Tag = LogTag.Client;

        private readonly ProtoSerialiser _serialiser = new ProtoSerialiser();
        private readonly ManualResetEvent _connected = new ManualResetEvent(false);
        private readonly BlockingCollection<ObjectWrap> _q = new BlockingCollection<ObjectWrap>(1000);
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _networkStream;
        private readonly PublisherSubscriptionStore _store;
        private readonly Thread _consumerThread;
        private string _topic;
        private int _messageLoss;
        private readonly string _endPoint;
        private string _name;
        private string _type;
        private long _count;

        internal PublisherClient(TcpClient tcpClient, NetworkStream networkStream, PublisherSubscriptionStore store)
        {
            _tcpClient = tcpClient;
            _networkStream = networkStream;
            _store = store;
            _endPoint = tcpClient.Client.RemoteEndPoint.ToString();
            _consumerThread = new Thread(Consumer) { IsBackground = true };
            _consumerThread.Start();
        }

        internal string Topic { get { return _topic; } }

        internal string EndPoint { get { return _endPoint; } }

        internal int MessageLoss { get { return Interlocked.CompareExchange(ref _messageLoss, 0, 0); } }

        public string Name { get { return _name; } }

        public string Type { get { return _type; } }

        public int Backlog { get { return _q.Count; } }

        public long MessageCount { get { return Interlocked.CompareExchange(ref _count, 0, 0); } }

        internal void Send(string topic, Type type, object message)
        {
            Interlocked.Increment(ref _count);

            try
            {
                if (!_q.TryAdd(new ObjectWrap {Topic = topic, Type = type, Object = message}, 10, _cancellation.Token))
                {
                    Interlocked.Increment(ref _messageLoss);
                }
                Log.Debug(Tag, "message queued to be sent..");
            }
            catch (OperationCanceledException) { }
            catch (InvalidOperationException) { }
        }

        internal void SetServerAck(Header header)
        {
            _topic = header.Topic;
            _name = header.Name;
            _type = header.Type;
            _connected.Set();
        }

        internal void Close()
        {
            _cancellation.Cancel();
            _tcpClient.Close();
            _consumerThread.Join();
        }

        private void Consumer()
        {
            Log.Info(Tag, "starting client consumer..");
            CancellationToken token = _cancellation.Token;

            _connected.WaitOne();
            while (true)
            {
                try
                {
                    ObjectWrap take = _q.Take(token);
                    Log.Debug(Tag, "dequeue message to send over wire..");

                    var header = new Header {Type = take.Type.Name, Topic = take.Topic};
                    _serialiser.Serialise(_networkStream, header);
                    _serialiser.Serialise(_networkStream, take.Type, take.Object);
                }
                catch (InvalidOperationException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (IOException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Log.Error(Tag, "UNEXPECTED_ERROR_CLI1: {0} : {1}", e.GetType(), e.Message);
                    break;
                }
            }

            _store.Remove(_tcpClient.Client);
            _tcpClient.Close();

            Log.Info(Tag, "exiting client consumer..");
        }
    }
}