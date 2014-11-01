using System.Diagnostics;

namespace ProtobufSockets.Stats
{
    class SystemStatsBuilder
    {
        int _threads;
        long _private;
        long _virtual;

        internal SystemStats Build()
        {
            return new SystemStats(_threads, _private, _virtual);
        }

        internal void ReadInFromCurrentProcess()
        {
            var p = Process.GetCurrentProcess();
            _threads = p.Threads.Count;
            _private = p.PrivateMemorySize64;
            _virtual = p.VirtualMemorySize64;
        }
    }
}