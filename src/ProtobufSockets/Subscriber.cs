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
    public class Subscriber : IDisposable
    {
        private const LogTag Tag = LogTag.Subscriber;

        private int _connected;
        private int _reconnect;
        private int _messageCount;
        private string _currentEndPoint;
        private readonly string[] _endPoints;

        private int _indexEndPoint = -1;
        private readonly IPEndPoint[] _endPoint;
        private readonly string _name;
        private TcpClient _tcpClient;
        private Thread _consumerThread;
        private Action<object> _action;
        private NetworkStream _networkStream;
        private string _topic;
        private bool _disposed;
        private Timer _reconnectTimer;
        private Type _type;
        private readonly ProtoSerialiser _serialiser = new ProtoSerialiser();
        private readonly object _disposeSync = new object();
        private readonly object _connectSync = new object();
        private readonly object _reconnectSync = new object();
        private readonly object _tcpClientSync = new object();
        private readonly object _typeSync = new object();
        private readonly object _statSync = new object();
        private Action<IPEndPoint> _connectedAction;

        public Subscriber(IPEndPoint[] endPoint, string name = null)
        {
            _endPoint = endPoint;
            _name = name;

            _endPoints = _endPoint.Select(e => e.ToString()).ToArray();
        }

        public void Subscribe<T>(Action<T> action)
        {
            Subscribe(null, action);
        }

        public void Subscribe<T>(string topic, Action<T> action, Action<IPEndPoint> connected = null)
        {
            _action = m => action((T)m);
            
            _connectedAction = connected;

            _topic = topic;
            
            lock (_typeSync)
                _type = typeof(T);

            Connect();
        }

        public void FailOver()
        {
            Reconnect();
        }

        public void Dispose()
        {
            lock (_disposeSync)
                _disposed = true;

            lock(_tcpClientSync)
            {
                if (_tcpClient != null)
                _tcpClient.Close();
            }

            lock (_connectSync)
                CleanExitConsumerThread();
        }

        private void Connect()
        {
			Monitor.Enter(_connectSync);
            try
            {
                _indexEndPoint++;
                if (_indexEndPoint == _endPoint.Length)
                    _indexEndPoint = 0;

                lock (_tcpClientSync)
                {
                    if (_tcpClient != null)
                        _tcpClient.Close();

                    _tcpClient = new TcpClient {NoDelay = true, LingerState = {Enabled = true, LingerTime = 0}};
                }

                lock (_statSync)
                    _currentEndPoint = _endPoint[_indexEndPoint].ToString();

                _tcpClient.Connect(_endPoint[_indexEndPoint]);

                _networkStream = _tcpClient.GetStream();

                _serialiser.Serialise(_networkStream, new Header { Topic = _topic, Type = _type.Name, Name = _name });
                var ack = _serialiser.Deserialize<string>(_networkStream);

				if (ack != "OK")
				{
					Reconnect();
					return;
				}

                CleanExitConsumerThread();

                _consumerThread = new Thread(Consume) { IsBackground = true };
                _consumerThread.Start();

                if (_connectedAction != null)
                    _connectedAction(_endPoint[_indexEndPoint]);

                Log.Debug(Tag, "publisher ack.. " + ack);
                Log.Debug(Tag, "subscribing started..");
            }
            catch (InvalidOperationException)
            {
                Log.Info(Tag, "cannot connect, reconnecting..");
                Reconnect();
            }
            catch (SocketException)
            {
                Log.Info(Tag, "cannot connect, reconnecting..");
                Reconnect();
            }
            catch (ProtoSerialiserException)
            {
                Log.Info(Tag, "cannot connect, reconnecting..");
                Reconnect();
            }
            catch (Exception e)
            {
                Log.Fatal(Tag, "UNEXPECTED_ERROR_SUB1: {0} : {1}", e.GetType(), e.Message);
                Reconnect();
            }
            finally
            {
                Monitor.Exit(_connectSync);
            }
        }

        private void CleanExitConsumerThread()
        {
            if (_consumerThread == null) return;

            try
            {
                if (!_consumerThread.Join(1000))
                {
                    _consumerThread.Abort();
                }
            }
            catch (Exception e)
            {
                Log.Fatal(Tag, "UNEXPECTED_ERROR_SUB2: {0} : {1}", e.GetType(), e.Message);
            }
        }

        private void Reconnect()
        {
            Interlocked.Increment(ref _reconnect);
            
            if (!Monitor.TryEnter(_reconnectSync)) return;
            try
            {
                try
                {
                    if (_reconnectTimer != null)
                        _reconnectTimer.Dispose();
                }
                catch (Exception e)
                {
                    Log.Fatal(Tag, "UNEXPECTED_ERROR_SUB3: {0} : {1}", e.GetType(), e.Message);
                }
                
                try
                {
                    lock (_tcpClientSync)
                    {
                        _tcpClient.Close();
                        _tcpClient = null;
                    }
                }
                catch (Exception e)
                {
                    Log.Fatal(Tag, "UNEXPECTED_ERROR_SUB4: {0} : {1}", e.GetType(), e.Message);
                }

                _reconnectTimer = new Timer(_ => Connect(), null, 1000, Timeout.Infinite);
            }
            finally
            {
                Monitor.Exit(_reconnectSync);
            }
        }

        private void Consume()
        {
            Type type;
            lock (_typeSync)
                type = _type;

            var typeName = type.Name;

            Log.Info(Tag, "consume started..");

            Interlocked.Exchange(ref _connected, 1);

            while (true)
            {
                try
                {
                    var header = _serialiser.Deserialize<Header>(_networkStream);

                    if (header.Type != typeName)
                    {
                        Log.Debug(Tag, "Ignoring unmatched type. (Subscribed with wrong type?)");
                        _serialiser.Chew(_networkStream);
                        continue;
                    }

                    var message = _serialiser.Deserialize(_networkStream, type);

                    Log.Debug(Tag, "got message..");

                    Interlocked.Increment(ref _messageCount);

                    _action(message);
                }
                catch (IOException)
                {
                    Log.Info(Tag, "cannot read from publisher..");
                    lock (_disposeSync) if (_disposed) break;
                    Reconnect();
                    break;
                }
                catch (ProtoSerialiserException)
                {
                    Log.Info(Tag, "cannot read from publisher..");
                    lock (_disposeSync) if (_disposed) break;
                    Reconnect();
                    break;
                }
                catch (Exception e)
                {
                    Log.Fatal(Tag, "UNEXPECTED_ERROR_SUB4: {0} : {1}", e.GetType(), e.Message);
                    lock (_disposeSync) if (_disposed) break;
                    Reconnect();
                    break;
                }
            }

            Interlocked.Exchange(ref _connected, 0);

            Log.Info(Tag, "consume exit..");
        }

        public SubscriberStats GetStats()
        {
            string currentEndPoint;
            string[] endPoints;
            string topic;
            string type;
            string name;

            lock (_statSync)
            {
                currentEndPoint = _currentEndPoint;
                endPoints = _endPoints;
                topic = _topic;
                type = _type.Name;
                name = _name;
            }

            return new SubscriberStats(Interlocked.CompareExchange(ref _connected, 0, 0) == 1,
                Interlocked.CompareExchange(ref _reconnect, 0, 0),
                Interlocked.CompareExchange(ref _messageCount, 0, 0),
                currentEndPoint,
                endPoints,
                topic,
                type,
                name);
        }
    }
}