using System.Collections.Generic;
using System.Linq;

namespace ProtobufSockets.Stats
{
    public class PublisherStats
    {
        private readonly List<PublisherClientStats> _clients;

        public PublisherStats(IEnumerable<PublisherClientStats> clients)
        {
            _clients = clients.ToList();
        }

        public IEnumerable<PublisherClientStats> Clients
        {
            get { return _clients; }
        }
    }
}