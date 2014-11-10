using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ProtobufSockets.Internal
{
    static class Log
    {
        static readonly Dictionary<LogTag, TraceSource> TraceSource = new Dictionary<LogTag, TraceSource>
        {
            { LogTag.Publisher, new TraceSource("ProtobufSockets.Publisher") },
            { LogTag.PublisherClient, new TraceSource("ProtobufSockets.PublisherClient") },
            { LogTag.Subscriber, new TraceSource("ProtobufSockets.Subscriber") },
            { LogTag.SubscriberClient, new TraceSource("ProtobufSockets.SubscriberClient") },
            { LogTag.ProtoSerialiser, new TraceSource("ProtobufSockets.ProtoSerialiser") },
        };
            

		internal static void Error(LogTag tag, string format, params object[] args)
		{
			Write(TraceEventType.Error, tag, format, args);
		}

        [Conditional("TRACE")]
        internal static void Info(LogTag tag, string format, params object[] args)
        {
            Write(TraceEventType.Information, tag, format, args);
        }

        [Conditional("DEBUG")]
        internal static void Debug(LogTag tag, string format, params object[] args)
        {
            Write(TraceEventType.Verbose, tag, format, args);
        }

        static void Write(TraceEventType level, LogTag tag, string format, params object[] args)
        {
            string s;
            try
            {
                s = string.Format(format, args);
            }
            catch (ArgumentNullException)
            {
                s = RecoverFormat(format, args);
            }
            catch (FormatException)
            {
                s = RecoverFormat(format, args);
            }

            var msg = string.Format("{0:yyyy-MM-dd HH:mm:ss} [{1}] {2}", DateTime.Now, tag, s);
            TraceSource[tag].TraceEvent(level, (int)tag, msg);
        }

        static string RecoverFormat(string format, IEnumerable<object> args)
        {
            var sb = new StringBuilder();

            if (format != null)
            {
                sb.Append(":");
                sb.Append(format);
            }

            if (args != null)
            {
                foreach (object o in args)
                {
                    if (o != null)
                    {
                        sb.Append(":");
                        sb.Append(o);
                    }
                }
            }

            return sb.ToString();
        }
    }
}