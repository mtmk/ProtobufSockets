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
		string _currentEndPoint;
		string _statTopic;
		string _statType;

		bool _disposed;
		int _indexEndPoint = -1;
		Timer _reconnectTimer;

		SubscriberClient _client;
		string _topic;
		Type _type;
		Action<object> _action;
		Action<IPEndPoint> _connectedAction;

        public Subscriber(IPEndPoint[] endPoints, string name = null)
        {
            _endPoints = endPoints;
            _name = name;
            _statEndPoints = _endPoints.Select(e => e.ToString()).ToArray();
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
			}

            Connect();
        }

        public void FailOver()
        {
            CloseClient();
        }

		public SubscriberStats GetStats()
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
			lock (_clientSync)
			{
				if (_client != null)
					count = _client.GetMessageCount();
			}

			return new SubscriberStats(Interlocked.CompareExchange(ref _connected, 0, 0) == 1,
				Interlocked.CompareExchange(ref _reconnect, 0, 0),
				count, currentEndPoint, _statEndPoints, topic, type, _name);
		}

        public void Dispose()
        {
			lock (_disposeSync) _disposed = true;
            CloseClient();
        }

        void CloseClient()
        {
            lock (_clientSync)
            {
                if (_client != null)
                    _client.Dispose();
                _client = null;
            }
        }

        void Connect()
        {
			lock (_disposeSync) if (_disposed) return;

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

			// Update stats
			lock (_statSync)
			{
				_currentEndPoint = _statEndPoints[indexEndPoint];
				_statType = type.FullName;
				_statTopic = topic;
			}

            CloseClient();

			// connect
			var client = SubscriberClient.Connect(
				_endPoints[indexEndPoint],
				_name,
				topic,
				type,
				action,
				Disconnected);

			if (client != null)
			{
				connectedAction (_endPoints [indexEndPoint]);
				lock (_clientSync)
					_client = client;
			}
			else
			{
				Reconnect();
			}
        }

		void Disconnected(IPEndPoint ep)
		{
			Interlocked.Exchange(ref _connected, 0);
			Reconnect();
		}

        void Reconnect()
        {
            Interlocked.Increment(ref _reconnect);
            
			if (!Monitor.TryEnter(_reconnectSync)) return;
            try
            {
                if (_reconnectTimer != null)
                    _reconnectTimer.Dispose();
                
                _reconnectTimer = new Timer(_ => Connect(), null, 700, Timeout.Infinite);
            }
            finally
            {
                Monitor.Exit(_reconnectSync);
            }
        }
    }
}