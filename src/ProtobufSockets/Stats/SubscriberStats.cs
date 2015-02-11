namespace ProtobufSockets.Stats
{
    public class SubscriberStats
    {
        public SubscriberStats(bool connected,
            int reconnectCount,
            int beatFailover,
            long messageCount,
            long beatCount,
            long totalMessageCount,
            string currentEndPoint,
            string[] endPoints,
            string topic,
            string type,
            string name,
            SystemStats systemStats)
        {
            Connected = connected;
            ReconnectCount = reconnectCount;
            BeatFailover = beatFailover;
            BeatCount = beatCount;
            MessageCount = messageCount;
            TotalMessageCount = totalMessageCount;
            CurrentEndPoint = currentEndPoint;
            EndPoints = endPoints;
            Topic = topic;
            Type = type;
            Name = name;
            SystemStats = systemStats;
        }

        public bool Connected { get; private set; }
        public int ReconnectCount { get; private set; }
        public int BeatFailover { get; private set; }
        public long BeatCount { get; private set; }
        public long MessageCount { get; private set; }
        public long TotalMessageCount { get; private set; }
        public string CurrentEndPoint { get; private set; }
        public string[] EndPoints { get; private set; }
        public string Topic { get; private set; }
        public string Type { get; private set; }
        public string Name { get; private set; }
        public SystemStats SystemStats { get; private set; }
    }
}