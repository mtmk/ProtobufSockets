using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace ProtobufSockets.Internal
{
    class PublisherClient
    {
        const LogTag Tag = LogTag.PublisherClient;

        readonly ProtoSerialiser _serialiser = new ProtoSerialiser();
        readonly BlockingCollection<ObjectWrap> _q = new BlockingCollection<ObjectWrap>(1000);
        readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        readonly TcpClient _tcpClient;
        readonly NetworkStream _networkStream;
        readonly PublisherSubscriptionStore _store;
        readonly Thread _consumerThread;
        readonly string _topic;
        readonly string _endPoint;
        readonly string _name;
        readonly string _type;
		int _messageLoss;
        long _count;

		internal PublisherClient(TcpClient tcpClient, NetworkStream networkStream, Header header, PublisherSubscriptionStore store)
        {
            _tcpClient = tcpClient;
            _networkStream = networkStream;
            _store = store;
			_topic = header.Topic;
			_name = header.Name;
			_type = header.Type;
			_endPoint = tcpClient.Client.RemoteEndPoint.ToString();

            _consumerThread = new Thread(Consumer) { IsBackground = true };
            _consumerThread.Start();
        }

		internal string Topic { get { return _topic; } }
        internal string EndPoint { get { return _endPoint; } }
        internal int MessageLoss { get { return Interlocked.CompareExchange(ref _messageLoss, 0, 0); } }
        internal string Name { get { return _name; } }
        internal string Type { get { return _type; } }
        internal int Backlog { get { return _q.Count; } }
        internal long MessageCount { get { return Interlocked.CompareExchange(ref _count, 0, 0); } }

        internal void Send(string topic, Type type, object message)
        {
            Interlocked.Increment(ref _count);

            try
            {
                if (!_q.TryAdd(new ObjectWrap {Topic = topic, Type = type, Object = message}, 10, _cancellation.Token))
                {
                    Interlocked.Increment(ref _messageLoss);
                }
                Log.Debug(Tag, "Message queued.");
            }
            catch (OperationCanceledException)
            {
                Log.Debug(Tag, "Send: OperationCanceledException");
            }
            catch (InvalidOperationException)
            {
                Log.Debug(Tag, "Send: InvalidOperationException");
            }
        }
			
        internal void Close()
        {
            Log.Debug(Tag, "Closing.");
            _cancellation.Cancel();
            _tcpClient.Close();
            _consumerThread.Join();
        }

        void Consumer()
        {
            Log.Info(Tag, "Starting client consumer [" + Thread.CurrentThread.ManagedThreadId + "]");
            CancellationToken token = _cancellation.Token;

            while (true)
            {
                try
                {
                    ObjectWrap take = _q.Take(token);
                    Log.Debug(Tag, "Got message to send over wire.");

                    var header = new Header {Type = take.Type.FullName, Topic = take.Topic};
                    _serialiser.Serialise(_networkStream, header);
                    _serialiser.Serialise(_networkStream, take.Type, take.Object);
                }
                catch (InvalidOperationException)
                {
                    Log.Debug(Tag, "Consumer: InvalidOperationException");
                    break;
                }
                catch (OperationCanceledException)
                {
                    Log.Debug(Tag, "Consumer: OperationCanceledException");
                    break;
                }
                catch (IOException e)
                {
                    Log.Debug(Tag, "Consumer: IOException: " + e.Message);
                    break;
                }
                catch (ProtoSerialiserException)
                {
                    Log.Debug(Tag, "Consumer: ProtoSerialiserException");
                    break;
                }
            }

            _store.Remove(_tcpClient.Client);
            _tcpClient.Close();

            Log.Info(Tag, "Exiting client consumer [" + Thread.CurrentThread.ManagedThreadId + "]");
        }
    }
}