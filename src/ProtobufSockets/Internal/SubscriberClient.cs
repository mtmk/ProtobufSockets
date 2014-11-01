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

		internal static SubscriberClient Connect(IPEndPoint endPoint,
			string name,
			string topic,
			Type type,
			Action<object> action,
			Action<IPEndPoint> disconnected)
		{
			try
			{
				var serialiser  = new ProtoSerialiser();
				var tcpClient = new TcpClient {NoDelay = true, LingerState = {Enabled = true, LingerTime = 0}};
				tcpClient.Connect(endPoint);
				var networkStream = tcpClient.GetStream();

				serialiser.Serialise(networkStream, new Header { Topic = topic, Type = type.FullName, Name = name });
				var ack = serialiser.Deserialize<string>(networkStream);

				if (ack != "OK")
				{
					return null;
				}

				Log.Debug(Tag, "publisher ack.. " + ack);
				Log.Debug(Tag, "subscribing started..");

				var client = new SubscriberClient(endPoint,
					type, action, disconnected,
					tcpClient, networkStream, serialiser);

				return client;
			}
			catch (InvalidOperationException)
			{
			}
			catch (SocketException)
			{
			}
			catch (ProtoSerialiserException)
			{
			}
			catch (Exception e)
			{
				Log.Fatal(Tag, "UNEXPECTED_ERROR_SUB_CLI1: {0} : {1}", e.GetType(), e.Message);
			}

			return null;
		}

		SubscriberClient(IPEndPoint endPoint,
			Type type,
			Action<object> action,
			Action<IPEndPoint> disconnected,
			TcpClient tcpClient,
			NetworkStream networkStream,
			ProtoSerialiser serialiser)
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

		public void Dispose()
		{
            if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1) return;

			Interlocked.Exchange(ref _disposed, 1);
			try { _networkStream.Close(); } catch { }
			try { _tcpClient.Close(); } catch { }
			_disconnected(_endPoint);
		}

		void Consume()
		{
			var typeName = _type.FullName;

			Log.Info(Tag, "consume started..");

			while (Interlocked.CompareExchange(ref _disposed, 0, 0) == 0)
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

					var message = _serialiser.Deserialize(_networkStream, _type);

					Log.Debug(Tag, "got message..");

					Interlocked.Increment(ref _messageCount);

					_action(message);
				}
				catch (IOException)
				{
					Log.Info(Tag, "cannot read from publisher..");
					break;
				}
				catch (ProtoSerialiserException)
				{
					Log.Info(Tag, "cannot read from publisher..");
					break;
				}
				catch (ObjectDisposedException)
				{
					Log.Info(Tag, "cannot read from publisher..");
					break;
				}
				catch (Exception e)
				{
					Log.Fatal(Tag, "UNEXPECTED_ERROR_SUB_CLI3: {0} : {1}", e.GetType(), e.Message);
					break;
				}
			}

			Dispose();

			Log.Info(Tag, "consume exit..");
		}

		internal long GetMessageCount()
		{
			return Interlocked.CompareExchange(ref _messageCount, 0, 0);
		}
	}
}

