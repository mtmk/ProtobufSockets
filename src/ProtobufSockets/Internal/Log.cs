using System;
using System.Diagnostics;

namespace ProtobufSockets.Internal
{
    internal static class Log
    {
        static readonly TraceSource TraceSource = new TraceSource("ProtobufSockets");

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
            var msg = string.Format("{0:yyyy-MM-dd HH:mm:ss} [{1}] {2}", DateTime.Now, tag, string.Format(format, args));
            TraceSource.TraceEvent(level, (int)tag, msg);
        }
    }
}