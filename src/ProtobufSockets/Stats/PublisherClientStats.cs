namespace ProtobufSockets.Stats
{
    public class PublisherClientStats
    {
        public PublisherClientStats(string name,
            int messageLoss,
            int backlog,
            string endPoint,
            string topic,
            long messageCount)
        {
            Topic = topic;
            EndPoint = endPoint;
            MessageLoss = messageLoss;
            Backlog = backlog;
            Name = name;
            MessageCount = messageCount;
        }

        public string Name { get; private set; }
        public int MessageLoss { get; private set; }
        public int Backlog { get; private set; }
        public string EndPoint { get; private set; }
        public string Topic { get; private set; }
        public long MessageCount { get; private set; }
    }
}