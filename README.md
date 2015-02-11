ProtobufSockets
===============

Simple .Net socket wrapper aimed at PubSub using protobuf-net. It supports simple round-robin failover and transparent connection recovery if the publisher goes down temporarily. It is fully managed and does not
need any native DLLs.

<a href="https://ci.appveyor.com/project/mtmk/protobufsockets"><img src="https://ci.appveyor.com/api/projects/status/github/mtmk/ProtobufSockets?branch=master&svg=true"/></a>

    PM> Install-Package ProtobufSockets

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

Main scenario
=============
A few backend services that needs to process light to medium load (tens to a few
hundred messages per second) data streams in a ressilient fashion. There maybe multiple
redundant publishers of the same data streams and multiple subscribers connecting to
one of these publishers. In case a publisher goes down subscribers connects to the next
available one. Silent netwoek failures are handled by using hearbeat messages between publisher
and subscriber pairs. Subscribers can also subscribe to predefined topics. A publisher
might be publishing messages of for multiple topics and wil only sends the ones subscriber
is interested in.



