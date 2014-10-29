using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ProtobufSockets;
using ProtobufSockets.Tests;

namespace pbspub
{
    class Pub
    {
        static int Usage()
        {
            Console.WriteLine(@"
Usage: pbspub <port>

Example:
 pbspub 23456
");
            return 2;
        }

        static int Main(string[] args)
        {
            if (args.Length != 1 || !Regex.IsMatch(args[0], @"^\d+$"))
                return Usage();

            int port = int.Parse(args[0]);

            int i = 0;
            Message message = Message.Large(1);
            using (var publisher = new Publisher(new IPEndPoint(IPAddress.Any, port)))
            {
                while (true)
                {
                    publisher.Publish("*", message);
                    Thread.Sleep(1);
                    
                    if (i++%1000 != 0) continue;
                    var clients = publisher.GetStats().Clients.ToList();
                    if (clients.Count <= 0) continue;
                    Console.WriteLine("# Total client [Loss:{0} Backlog:{1}]",
                        clients.Sum(c => c.MessageLoss),
                        clients.Sum(c => c.Backlog));
                }
            }

            return 0;
        }

    }
}
