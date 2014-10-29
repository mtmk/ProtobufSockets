ProtobufSockets
===============

Simple .Net socket wrapper aimed at PubSub using protobuf-net

Example
=======

    var r = new ManualResetEvent(false);
    var publisher = new Publisher();
    var subscriber = new Subscriber(new []{publisher.EndPoint});

    subscriber.Subscribe<Message>("*", m =>
    {
        r.Set();
        Assert.Equal("payload1", m.Payload);
    });

    publisher.Publish("*", new Message {Payload = "payload1"});

    Assert.True(r.WaitOne(3000), "Timed out");

    publisher.Dispose();
    subscriber.Dispose();

