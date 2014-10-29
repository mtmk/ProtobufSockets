using ProtoBuf;

namespace ProtobufSockets.Tests
{
    [ProtoContract]
    public class Message
    {
        [ProtoMember(1)] public string Payload { get; set; }

        [ProtoMember(21)] public string S1 { get; set; }
        [ProtoMember(22)] public string S2 { get; set; }
        [ProtoMember(23)] public string S3 { get; set; }
        [ProtoMember(24)] public string S4 { get; set; }
        [ProtoMember(25)] public string S5 { get; set; }
        [ProtoMember(26)] public string S6 { get; set; }
        [ProtoMember(27)] public string S7 { get; set; }
        [ProtoMember(28)] public string S8 { get; set; }
        [ProtoMember(29)] public string S9 { get; set; }

        [ProtoMember(31)] public int X1 { get; set; }
        [ProtoMember(32)] public int X2 { get; set; }
        [ProtoMember(33)] public int X3 { get; set; }
        [ProtoMember(34)] public int X4 { get; set; }
        [ProtoMember(35)] public int X5 { get; set; }
        [ProtoMember(36)] public int X6 { get; set; }
        [ProtoMember(37)] public int X7 { get; set; }
        [ProtoMember(38)] public int X8 { get; set; }
        [ProtoMember(39)] public int X9 { get; set; }

        [ProtoMember(41)] public double Y1 { get; set; }
        [ProtoMember(42)] public double Y2 { get; set; }
        [ProtoMember(43)] public double Y3 { get; set; }
        [ProtoMember(44)] public double Y4 { get; set; }
        [ProtoMember(45)] public double Y5 { get; set; }
        [ProtoMember(46)] public double Y6 { get; set; }
        [ProtoMember(47)] public double Y7 { get; set; }
        [ProtoMember(48)] public double Y8 { get; set; }
        [ProtoMember(49)] public double Y9 { get; set; }

        public static Message Large(int k)
        {
            return new Message
            {
                Payload = new string('x', 100*k),
                S1 = new string('x', 100*k),
                S2 = new string('x', 100*k),
                S3 = new string('x', 100*k),
                S4 = new string('x', 100*k),
                S5 = new string('x', 100*k),
                S6 = new string('x', 100*k),
                S7 = new string('x', 100*k),
                S8 = new string('x', 100*k),
                S9 = new string('x', 100*k),
                X1 = 12345678,
                X2 = 12345678,
                X3 = 12345678,
                X4 = 12345678,
                X5 = 12345678,
                X6 = 12345678,
                X7 = 12345678,
                X8 = 12345678,
                X9 = 12345678,
                Y1 = 12345678.12345678,
                Y2 = 12345678.12345678,
                Y3 = 12345678.12345678,
                Y4 = 12345678.12345678,
                Y5 = 12345678.12345678,
                Y6 = 12345678.12345678,
                Y7 = 12345678.12345678,
                Y8 = 12345678.12345678,
                Y9 = 12345678.12345678,
            };
        }
    }
}