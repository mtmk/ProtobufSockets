namespace ProtobufSockets.Internal
{
    internal static class Topic
    {
        internal static bool Match(string topic, string test)
        {
            if (test == null || topic == null) return true;

            test = test.ToLowerInvariant();
            topic = topic.ToLowerInvariant();

            foreach (var t in topic.Split(','))
            {
                var t1 = t;
                if (t.EndsWith("*"))
                {
                    t1 = t.TrimEnd('*', '.');
                    if (test.StartsWith(t1)) return true;
                }

                if (test == t1) return true;
            }

            return false;
        }
    }
}