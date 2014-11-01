using System;
using System.IO;
using ProtoBuf;
using ProtoBuf.Meta;

namespace ProtobufSockets.Internal
{
    class ProtoSerialiser
    {
        readonly RuntimeTypeModel _model = RuntimeTypeModel.Default;

        internal T Deserialize<T>(Stream stream) where T : class
        {
            var o = Serializer.DeserializeWithLengthPrefix<T>(stream, PrefixStyle.Base128);
            if (o == null)
            {
                throw new ProtoSerialiserException();
            }
            return o;
        }

        internal void Chew(Stream stream)
        {
            var o = Serializer.DeserializeWithLengthPrefix<string>(stream, PrefixStyle.Base128);
            if (o == null)
            {
                throw new ProtoSerialiserException();
            }
        }

        internal object Deserialize(Stream stream, Type type)
        {
            var o = _model.DeserializeWithLengthPrefix(stream, null, type, PrefixStyle.Base128, 0);
            if (o == null)
            {
                throw new ProtoSerialiserException();
            }
            return o;
        }

        internal void Serialise<T>(Stream stream, T obj)
        {
            Serializer.SerializeWithLengthPrefix(stream, obj, PrefixStyle.Base128);
        }

        internal void Serialise(Stream stream, Type type, object obj)
        {
            _model.SerializeWithLengthPrefix(stream, obj, type, PrefixStyle.Base128, 0);
        }
    }
}