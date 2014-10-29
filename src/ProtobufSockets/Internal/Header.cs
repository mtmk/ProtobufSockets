using ProtoBuf;

namespace ProtobufSockets.Internal
{
    [ProtoContract]
    public class Header
    {
        [ProtoMember(1)]
        public string Type { get; set; }
        [ProtoMember(2)]
        public string Topic { get; set; }
        [ProtoMember(3)]
        public string Name { get; set; }
    }
}