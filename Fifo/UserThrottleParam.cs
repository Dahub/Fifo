using System.Collections.Generic;

namespace Fifo
{
    public class UserThrottleParam
    {
        public UserThrottleParam()
        {
            RoutesFrequencies = new Dictionary<string, int>();
        }

        public string UserName { get; set; }
        public IDictionary<string, int> RoutesFrequencies { get; set; }
    }
}
