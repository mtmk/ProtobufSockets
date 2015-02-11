using System;
using System.Linq;
using System.Net;
using System.Threading;
using ProtobufSockets.Internal;
using ProtobufSockets.Stats;

namespace ProtobufSockets
{
    public class Subscriber : IDisposable
    {
        const LogTag Tag = LogTag.Subscriber;

		readonly object _reconnectSync = new object();
		readonly object _typeSync = new object();
		readonly object _statSync = new object();
		readonly object _indexSync = new object();
		readonly object _clientSync = new object();
		readonly object _disposeSync = new object();

		readonly string[] _statEndPoints;
		readonly IPEndPoint[] _endPoints;
		readonly string _name;

		int _connected;
        int _reconnect;
        long _lastCount;
        long _totalCount;
		string _currentEndPoint;
		string _statTopic;
		string _statType;

		bool _disposed;
		int _indexEndPoint = -1;
		long _beatCount = -1;
		Timer _reconnectTimer;

		SubscriberClient _client;
		string _topic;
		Type _type;
		Action<object> _action;
		Action<IPEndPoint> _connectedAction;
        readonly Timer _beatTimer;

        public Subscriber(IPEndPoint[] endPoints, string name = null)
        {
            _endPoints = endPoints;
            _name = name;
            _statEndPoints = _endPoints.Select(e => e.ToString()).ToArray();

            _beatTimer = new Timer(_ => CheckBeat(), null, 10*1000, 10*1000);
        }

        private void CheckBeat()
        {
            lock (_clientSync)
            {
                if (_client == null) return;
                long beatCount = _client.GetBeatCount();
                long current = Interlocked.Exchange(ref _beatCount, beatCount);

                if (current != beatCount) return;

                Log.Debug(Tag, "Lost the heart beat, will failover..");
                FailOver();
            }
        }

        public void Subscribe<T>(Action<T> action)
        {
            Subscribe(null, action);
        }

        public void Subscribe<T>(string topic, Action<T> action, Action<IPEndPoint> connected = null)
        {
			lock (_typeSync)
			{
				_type = typeof(T);
				_topic = topic;
				_action = m => action ((T)m);
				_connectedAction = ep => {
					Interlocked.Exchange (ref _connected, 1);
					if (connected != null) connected(ep);
				};

                Log.Info(Tag, "Subscribing to " + (topic ?? "<null>") + " as " + (_name ?? "<null>") + " with type " + _type.FullName);
            }

            Connect();
        }

        public void FailOver()
        {
            CloseClient();
        }

        public SubscriberStats GetStats(bool withSystemStats = false)
		{
			string currentEndPoint;
			string topic;
			string type;
			lock (_statSync)
			{
				currentEndPoint = _currentEndPoint;
				topic = _statTopic;
				type = _statType;
			}

			long count = 0;
			long beatCount = 0;
			long totalCount;
			lock (_clientSync)
			{
			    if (_client != null)
			    {
			        count = _client.GetMessageCount();
			        beatCount = _client.GetBeatCount();
			    }

			    if (count < _lastCount)
			        _lastCount = 0;

                _totalCount += count - _lastCount;
			    
                _lastCount = count;

			    totalCount = _totalCount;
			}

            var statsBuilder = new SystemStatsBuilder();
            if (withSystemStats)
                statsBuilder.ReadInFromCurrentProcess();

			return new SubscriberStats(Interlocked.CompareExchange(ref _connected, 0, 0) == 1,
				Interlocked.CompareExchange(ref _reconnect, 0, 0),
				count, beatCount, totalCount, currentEndPoint, _statEndPoints, topic, type, _name, statsBuilder.Build());
		}

        public void Dispose()
        {
			lock (_disposeSync) _disposed = true;
            _beatTimer.Dispose();
            CloseClient();
        }

        void CloseClient()
        {
            Log.Debug(Tag, "Closing client.");
            Interlocked.Exchange(ref _connected, 0);
            lock (_clientSync)
            {
                if (_client != null)
                {
                    Log.Debug(Tag, "Disposing client.");
                    _client.Dispose();
                }
                _client = null;
            }
        }

        void Connect()
        {
			lock (_disposeSync) if (_disposed) return;

            Log.Debug(Tag, "Connecting..");

			// copy mutables
			Type type;
			string topic;
			Action<object> action;
			Action<IPEndPoint> connectedAction;
			lock (_typeSync)
			{
				type = _type;
				topic = _topic;
				action = _action;
				connectedAction = _connectedAction;
			}

			// next endpoint
			int indexEndPoint;
			lock(_indexSync)
			{
                _indexEndPoint++;
                if (_indexEndPoint == _endPoints.Length)
                    _indexEndPoint = 0;
				indexEndPoint = _indexEndPoint;
			}
            IPEndPoint endPoint = _endPoints[indexEndPoint];

            Log.Info(Tag, "Connecting to " + endPoint);

			// Update stats
			lock (_statSync)
			{
				_currentEndPoint = _statEndPoints[indexEndPoint];
				_statType = type.FullName;
				_statTopic = topic;
			}

            CloseClient();

            var client = SubscriberClient.Connect(endPoint, _name, topic, type, action, Disconnected);

			if (client != null)
			{
			    Log.Info(Tag, "Connection successful to " + endPoint);
				connectedAction(endPoint);
				lock (_clientSync)
					_client = client;
			}
			else
			{
                Log.Info(Tag, "Connection unsuccessful for " + endPoint + ". Will reconnect..");
                Reconnect();
			}
        }

		void Disconnected(IPEndPoint ep)
		{
            Log.Debug(Tag, "Disconnected from " + ep);
            Reconnect();
		}

        void Reconnect()
        {
			if (!Monitor.TryEnter(_reconnectSync)) return; // do not queue up simultaneous calls
            try
            {
                Interlocked.Exchange(ref _connected, 0);
                Interlocked.Increment(ref _reconnect);

                if (_reconnectTimer != null)
                    _reconnectTimer.Dispose();

                Log.Debug(Tag, "Running reconnect timer..");
                _reconnectTimer = new Timer(_ => Connect(), null, 700, Timeout.Infinite);
            }
            finally
            {
                Monitor.Exit(_reconnectSync);
            }
        }
    }
}