using System.Collections.Generic;
using System.Net;
using System.Threading;
using NUnit.Framework;

namespace ProtobufSockets.Tests
{
	[TestFixture]
    public class PubSubTests
    {
		const int Timeout = 3000;

		[Test]
		public void Publisher_starts_with_an_ephemeral_port()
		{
			var r = new ManualResetEvent(false);

			var publisher = new Publisher();

			var subscriber = new Subscriber(new []{publisher.EndPoint});

			subscriber.Subscribe<Message>("*", m =>
				{
					r.Set();
					Assert.AreEqual("payload1", m.Payload);
				});

			publisher.Publish("*", new Message {Payload = "payload1"});

			Assert.IsTrue(r.WaitOne(Timeout), "Timed out");

			publisher.Dispose();
			subscriber.Dispose();
		}

		[Test]
		public void Subscriber_client_induced_failover()
		{
			var publisher1 = new Publisher();
			var publisher2 = new Publisher();

			var r = new [] {new ManualResetEvent(false), new ManualResetEvent(false)};
			var rc = new Dictionary<IPEndPoint, ManualResetEvent>
			{
				{publisher1.EndPoint, new ManualResetEvent(false)},
				{publisher2.EndPoint, new ManualResetEvent(false)},
			};
			int c = 0;

			var subscriber = new Subscriber(new []{publisher1.EndPoint, publisher2.EndPoint});

			subscriber.Subscribe<Message>("*", m =>
				{
					var c1 = Interlocked.Increment(ref c);
					r[c1-1].Set();
					Assert.AreEqual("payload" + c1, m.Payload);
				}, ep => rc[ep].Set());

			Assert.IsTrue (rc [publisher1.EndPoint].WaitOne (Timeout));
			publisher1.Publish("*", new Message {Payload = "payload1"});
			Assert.IsTrue(r[0].WaitOne(Timeout), "Timed out");

			subscriber.FailOver ();

			Assert.IsTrue (rc [publisher2.EndPoint].WaitOne (Timeout));
			publisher2.Publish("*", new Message {Payload = "payload2"});
			Assert.IsTrue(r[1].WaitOne(Timeout), "Timed out");

			publisher1.Dispose();
			publisher2.Dispose();
			subscriber.Dispose();
		}

        [Test]
        public void Publish_different_topics()
        {
            var r1 = new ManualResetEvent(false);
            var r2 = new ManualResetEvent(false);

            var publisher = new Publisher();

            var subscriber1 = new Subscriber(new[] { publisher.EndPoint });
            subscriber1.Subscribe<Message>("topic1", m =>
            {
                r1.Set();
                Assert.AreEqual("payload1", m.Payload);
            });

            var subscriber2 = new Subscriber(new[] { publisher.EndPoint });
            subscriber2.Subscribe<Message>("topic2", m =>
            {
                r2.Set();
                Assert.AreEqual("payload2", m.Payload);
            });

            publisher.Publish("topic1", new Message { Payload = "payload1" });
            publisher.Publish("topic2", new Message { Payload = "payload2" });

			Assert.IsTrue(r1.WaitOne(Timeout), "Timed out");
			Assert.IsTrue(r2.WaitOne(Timeout), "Timed out");

            publisher.Dispose();
            subscriber1.Dispose();
            subscriber2.Dispose();
        }

        [Test]
        public void Multiple_publishers_and_subscribers_with_failover()
        {
            var r1 = new[] { new ManualResetEvent(false), new ManualResetEvent(false), new ManualResetEvent(false) };
            var r2 = new[] { new ManualResetEvent(false), new ManualResetEvent(false), new ManualResetEvent(false) };
            var c1 = 0;
            var c2 = 0;

            var publisher1 = new Publisher();
            var publisher2 = new Publisher();
            var publisher3 = new Publisher();

            var rc1 = new Dictionary<IPEndPoint, ManualResetEvent>
            {
                {publisher1.EndPoint, new ManualResetEvent(false)},
                {publisher2.EndPoint, new ManualResetEvent(false)},
                {publisher3.EndPoint, new ManualResetEvent(false)},
            };
            
            var rc2 = new Dictionary<IPEndPoint, ManualResetEvent>
            {
                {publisher1.EndPoint, new ManualResetEvent(false)},
                {publisher2.EndPoint, new ManualResetEvent(false)},
                {publisher3.EndPoint, new ManualResetEvent(false)},
            };

            var endPoints = new[] { publisher1.EndPoint, publisher2.EndPoint, publisher3.EndPoint };

            var subscriber1 = new Subscriber(endPoints);
            subscriber1.Subscribe<Message>(null, m =>
            {
                int c = Interlocked.Increment(ref c1);
                Assert.AreEqual("payload" + c, m.Payload);
                 r1[c-1].Set();
            }, ep => rc1[ep].Set());

            var subscriber2 = new Subscriber(endPoints);
            subscriber2.Subscribe<Message>(null, m =>
            {
                int c = Interlocked.Increment(ref c2);
                Assert.AreEqual("payload" + c, m.Payload);
                r2[c-1].Set();
            }, ep => rc2[ep].Set());

			Assert.True(rc1[publisher1.EndPoint].WaitOne(Timeout), "Timed out");
			Assert.True(rc2[publisher1.EndPoint].WaitOne(Timeout), "Timed out");
            publisher1.Publish(new Message { Payload = "payload1" });
			Assert.True(r1[0].WaitOne(Timeout), "Timed out");
			Assert.True(r2[0].WaitOne(Timeout), "Timed out");
            publisher1.Dispose();

            // should fail over to publisher2
			Assert.True(rc1[publisher2.EndPoint].WaitOne(Timeout), "Timed out");
			Assert.True(rc2[publisher2.EndPoint].WaitOne(Timeout), "Timed out");
            publisher2.Publish(new Message { Payload = "payload2" });
			Assert.True(r1[1].WaitOne(Timeout), "Timed out");
			Assert.True(r2[1].WaitOne(Timeout), "Timed out");
            publisher2.Dispose();

            // should fail over to publisher3
			Assert.True(rc1[publisher3.EndPoint].WaitOne(Timeout), "Timed out");
			Assert.True(rc2[publisher3.EndPoint].WaitOne(Timeout), "Timed out");
            publisher3.Publish(new Message { Payload = "payload3" });
			Assert.True(r1[2].WaitOne(Timeout), "Timed out");
			Assert.True(r2[2].WaitOne(Timeout), "Timed out");
            publisher3.Dispose();

            subscriber1.Dispose();
            subscriber2.Dispose();
        }
    }
}