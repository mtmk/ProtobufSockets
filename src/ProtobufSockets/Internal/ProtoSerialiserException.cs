using System;

namespace ProtobufSockets.Internal
{
    class ProtoSerialiserException : Exception
    {
        public ProtoSerialiserException()
        {
        }

        public ProtoSerialiserException(Exception exception)
            : base("Protobuf error", exception)
        {
        }
    }
}