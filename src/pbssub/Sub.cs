using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ProtoBuf;
using ProtobufSockets;
using ProtobufSockets.Tests;

namespace pbssub
{
    class Sub
    {
        static int Usage()
        {
            Console.WriteLine(@"
Usage: pbssub <ip-address> <port>

Example:
 pbssub 127.0.0.1 23456
");
            return 2;
        }

        static int Main(string[] args)
        {
            if (args.Length != 2
                || !Regex.IsMatch(args[0], @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$")
                || !Regex.IsMatch(args[1], @"^\d+$"))
                return Usage();

            IPAddress ipAddress = IPAddress.Parse(args[0]);
            int port = int.Parse(args[1]);

            const int max = 1000;
            var samples = new int[max];

            var subscriber = new Subscriber(new[]
            {
                new IPEndPoint(ipAddress, port),
            });

            int i = -1;
            Stopwatch sw = Stopwatch.StartNew();
            subscriber.Subscribe<Message>("*", m =>
            {
                int exchange = Interlocked.CompareExchange(ref i, -1, max - 1);
                if (exchange == max - 1)
                {
                    double seconds = sw.Elapsed.TotalSeconds;
                    Console.WriteLine("# {0:0.00} MB/s ({1:0.0} messages/s)",
                        (samples.Sum() / seconds) / (1024 * 1024),
                        (samples.Length / seconds));
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
            return 0;
        }
    }
}
