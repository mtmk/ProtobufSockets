ProtobufSockets
===============

Simple .Net socket wrapper aimed at PubSub using protobuf-net. It supports simple round-robin failover and transparent connection recovery if the publisher goes down temporarily.

<a href="https://ci.appveyor.com/project/mtmk/protobufsockets"><img src="https://ci.appveyor.com/api/projects/status/github/mtmk/ProtobufSockets?branch=master&svg=true"/></a>

Example
=======

Here is the basic scenario where you can have an example up and running in a few minutes:

Define your messages:
```cs
[ProtoContract]
public class Message
{
    [ProtoMember(1)]
    public string Payload { get; set; }
}
```

Run publisher:
```cs
static void Main()
{
    using (var publisher = new Publisher(new IPEndPoint(IPAddress.Any, 34567)))
    {
        int i = 1;
        while (true)
        {
            Console.WriteLine("Publishing message #" + i);
            publisher.Publish(new Message {Payload = "payload" + i});
            i++;
            Thread.Sleep(1000);
        }
    }
}
```

Run subscriber:
```cs
static void Main()
{
    using (var subscriber = new Subscriber(new[] {new IPEndPoint(IPAddress.Loopback, 34567)}))
    {
        subscriber.Subscribe<Message>(m =>
        {
            Console.WriteLine("Received: {0}", m.Payload);
        });

        Console.ReadLine();
    }

}
```

Disclamer
=========
This library uses syncronous sockets (i.e. Not IOCP) and keeps hold of a thread
per client connection. It is not recommended if you have many clients
(e.g. more than 50) especially connecting and disconnecting frequently.

Main scenario
=============
A few backend services that needs to process light to medium load (tens to a few
hundred messages per second) in a ressilient fashion (i.e. subscriber
can connect to an equal publisher if one fails). Also no overall message consistency
is guaranteed either, so durign failover yoou might loose a few messages.
Your application should be tolerant for this occasional message loss as well.

Other libraries you might want to check
=======================================
If some of the limitations aren't acceptable for you, please see these other libraries.
This is a quick list of other libraries I found on NuGet which might provide more
general functionality:

(Note that I haven't used them myself, only found them on NuGet and they looked 'OK',
so please don't blame me if they don't work. Do your own research.)

https://www.nuget.org/packages/RedFoxMQ.Serialization.ProtoBuf/

https://www.nuget.org/packages/Stacks.ProtoBuf/


