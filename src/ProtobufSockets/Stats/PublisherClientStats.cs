namespace ProtobufSockets.Stats
{
    public class PublisherClientStats
    {
        public PublisherClientStats(string name,
            int messageLoss,
            int backlog,
            string endPoint,
            string topic,
            string type,
            long messageCount)
        {
			Name = name;
			MessageLoss = messageLoss;
			Backlog = backlog;
			EndPoint = endPoint;
            Topic = topic;
            Type = type;
            MessageCount = messageCount;
        }

        public string Name { get; private set; }
        public int MessageLoss { get; private set; }
        public int Backlog { get; private set; }
        public string EndPoint { get; private set; }
        public string Topic { get; private set; }
        public string Type { get; private set; }
        public long MessageCount { get; private set; }
    }
}