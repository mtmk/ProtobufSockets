namespace ProtobufSockets.Stats
{
    public class SystemStats
    {
        public SystemStats(int threads, long privateMemory, long virtualMemory)
        {
            VirtualMemory = virtualMemory;
            PrivateMemory = privateMemory;
            Threads = threads;
        }

        public int Threads { get; private set; }
        public long PrivateMemory { get; private set; }
        public long VirtualMemory { get; private set; }
    }
}