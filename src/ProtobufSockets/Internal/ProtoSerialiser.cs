using System;
using System.IO;
using ProtoBuf;
using ProtoBuf.Meta;

namespace ProtobufSockets.Internal
{
    class ProtoSerialiser
    {
        const LogTag Tag = LogTag.SubscriberClient;

        readonly RuntimeTypeModel _model = RuntimeTypeModel.Default;

        internal T Deserialize<T>(Stream stream) where T : class
        {
            T o = null;

            Wrapped(() =>
            {
                o = Serializer.DeserializeWithLengthPrefix<T>(stream, PrefixStyle.Base128);
            });

            if (o == null)
            {
                throw new ProtoSerialiserException();
            }
            return o;
        }

        internal object Deserialize(Stream stream, Type type)
        {
            object o = null;

            Wrapped(() =>
            {
                o = _model.DeserializeWithLengthPrefix(stream, null, type, PrefixStyle.Base128, 0);
            });

            if (o == null)
            {
                throw new ProtoSerialiserException();
            }
            return o;
        }

        internal void Serialise<T>(Stream stream, T obj)
        {
            Wrapped(() => Serializer.SerializeWithLengthPrefix(stream, obj, PrefixStyle.Base128));
        }

        internal void Serialise(Stream stream, Type type, object obj)
        {
            Wrapped(() => _model.SerializeWithLengthPrefix(stream, obj, type, PrefixStyle.Base128, 0));
        }

        static void Wrapped(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException e)
            {
                Log.Error(Tag, e.Message);
                throw new ProtoSerialiserException(e);
            }
        }
    }
}