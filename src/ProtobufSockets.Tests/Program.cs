using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using ProtoBuf;
using Xunit;

namespace ProtobufSockets.Tests
{
    public class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "test")
            {
				new PubSubTests ().Multiple_publishers_and_subscribers_with_failover ();
				new PubSubTests ().Publisher_starts_with_an_ephemeral_port ();
				new PubSubTests ().Publish_different_topics ();
				return 0;
           } 

            const int port = 23456;

            if (args.Length > 0 && args[0] == "sub")
            {
                new Program().Sub(port);
            }

            if (args.Length > 0 && args[0] == "pub1")
            {
                new Program().Pub(port);
            }
            if (args.Length > 0 && args[0] == "pub2")
            {
                new Program().Pub(port + 1);
            }
            if (args.Length > 0 && args[0] == "pub3")
            {
                new Program().Pub(port + 2);
            }


            if (args.Length > 0 && args[0] == "sub-fast")
            {
                new Program().SubFast(port);
            }
            if (args.Length > 0 && args[0] == "pub-fast")
            {
                new Program().PubFast(port);
            }

            return 0;
        }

        public void Sub(int port)
        {
            var subscriber = new Subscriber(new[]
            {
                new IPEndPoint(IPAddress.Loopback, port),
                new IPEndPoint(IPAddress.Loopback, port + 1),
                new IPEndPoint(IPAddress.Loopback, port + 2),
            });

            subscriber.Subscribe<Message>("*", m =>
            {
                Console.WriteLine(m.Payload);
            });

            var r = new ManualResetEvent(false);
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; r.Set(); };
            r.WaitOne();

            subscriber.Dispose();
        }

        public void Pub(int port)
        {
            var r = new ManualResetEvent(false);
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; r.Set(); };

            var publisher = new Publisher(new IPEndPoint(IPAddress.Any, port));
            int i = 0;
            while (true)
            {
                publisher.Publish("*", new Message { Payload = "payload" + i++ });
                if (r.WaitOne(500)) break;

                if (i % 3 == 0)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(publisher.GetStats(), Formatting.Indented));
                }
            }

            publisher.Dispose();
        }

        public void PubFast(int port)
        {
            var r = new ManualResetEvent(false);
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; r.Set(); };

            int i = 0;
            using (var publisher = new Publisher(new IPEndPoint(IPAddress.Any, port)))
                while (true)
                {
                    Message message = Message.Large(1);
                    publisher.Publish("*", message);
                    if (r.WaitOne(1)) break;
                    if (i++%100 == 0)
                    {
                        var clients = publisher.GetStats().Clients.ToList();
                        if (clients.Count > 0)
                        {
                            Console.WriteLine("LOSS:{0} BACK:{1}",
                                clients.Sum(c=>c.MessageLoss),
                                clients.Sum(c => c.Backlog));
                        }
                    }
                }
        }

        public void SubFast(int port)
        {
            const int max = 1000;
            var samples = new int[max];

            var subscriber = new Subscriber(new[]
            {
                new IPEndPoint(IPAddress.Loopback, port),
            });

            int i = -1;
            Stopwatch sw = Stopwatch.StartNew();
            subscriber.Subscribe<Message>("*", m =>
            {
                int exchange = Interlocked.CompareExchange(ref i, -1, max - 1);
                if (exchange == max - 1)
                {
                    Console.WriteLine("### {0:0.00} MB/s", (samples.Sum()/sw.Elapsed.TotalSeconds)/(1024*1024));
                    Console.WriteLine("### {0:0.00} messages/s", (samples.Length/sw.Elapsed.TotalSeconds));
                    sw.Restart();
                }

                int c = Interlocked.Increment(ref i);
                var memoryStream = new MemoryStream();
                Serializer.SerializeWithLengthPrefix(memoryStream, m, PrefixStyle.Base128);
                byte[] bytes = memoryStream.ToArray();
                samples[c] = bytes.Length;
            });

            var r = new ManualResetEvent(false);
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; r.Set(); };
            r.WaitOne();

            subscriber.Dispose();
        }

        public int SizeOfLargeMessage()
        {
            var memoryStream = new MemoryStream();
            Serializer.SerializeWithLengthPrefix(memoryStream, Message.Large(10), PrefixStyle.Base128);
            byte[] bytes = memoryStream.ToArray();
            return bytes.Length;
        }
    }
}