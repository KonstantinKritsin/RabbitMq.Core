using System.Collections.Generic;

namespace Esb.RabbitMq.Core
{
    public class RequestMessage
    {
        public int V { get; set; }
        public Dictionary<string, string> P { get; set; }
    }
}