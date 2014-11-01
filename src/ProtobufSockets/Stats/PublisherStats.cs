using System.Collections.Generic;
using System.Linq;

namespace ProtobufSockets.Stats
{
    public class PublisherStats
    {
        private readonly List<PublisherClientStats> _clients;

        public PublisherStats(IEnumerable<PublisherClientStats> clients, SystemStats systemStats)
        {
            SystemStats = systemStats;
            _clients = clients.ToList();
            NumberOfSubscribers = _clients.Count;
        }

        public IEnumerable<PublisherClientStats> Clients { get { return _clients; } }
        public int NumberOfSubscribers { get; private set; }
        public SystemStats SystemStats { get; private set; }
    }
}