namespace ProtobufSockets.Stats
{
    public class SubscriberStats
    {
        public SubscriberStats(bool connected,
            int reconnectCount,
            int messageCount,
            string currentEndPoint,
            string[] endPoints,
            string topic,
            string type,
            string name)
        {
            Connected = connected;
            ReconnectCount = reconnectCount;
            MessageCount = messageCount;
            CurrentEndPoint = currentEndPoint;
            EndPoints = endPoints;
            Topic = topic;
            Type = type;
            Name = name;
        }

        public bool Connected { get; private set; }
        public int ReconnectCount { get; private set; }
        public int MessageCount { get; private set; }
        public string CurrentEndPoint { get; private set; }
        public string[] EndPoints { get; private set; }
        public string Topic { get; private set; }
        public string Type { get; private set; }
        public string Name { get; private set; }
    }
}