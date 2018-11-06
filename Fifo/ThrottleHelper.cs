using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Fifo
{
    public class ThrottleHelper
    {
        private ConcurrentDictionary<string, ConcurrentQueue<QueueElem>> _stack;
        private ConcurrentDictionary<string, IDictionary<string, int>> _userRoutes;

        private readonly long _queueLivingMs = 60 * 1000;

        public ThrottleHelper() { }

        public ThrottleHelper(params UserThrottleParam[] userParameters)
        {
            Init(userParameters);
        }

        public ThrottleHelper(long queueLivingMs, params UserThrottleParam[] userParameters)
        {
            _queueLivingMs = queueLivingMs;
            Init(userParameters);
        }

        public void Init(UserThrottleParam[] userParameters)
        {
            _stack = new ConcurrentDictionary<string, ConcurrentQueue<QueueElem>>();
            _userRoutes = new ConcurrentDictionary<string, IDictionary<string, int>>();

            foreach (var up in userParameters)
            {
                _stack.TryAdd(up.UserName, new ConcurrentQueue<QueueElem>());
                _userRoutes.TryAdd(up.UserName, new Dictionary<string, int>());

                _userRoutes[up.UserName] = up.RoutesFrequencies.ToDictionary(r => r.Key, r => r.Value);
            }
        }

        public void Add(string userName, string route)
        {
            if (!_userRoutes.ContainsKey(userName))
                return;

            if (!_userRoutes[userName].ContainsKey(route))
                return;

            _stack[userName].Enqueue(new QueueElem(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), route));
        }

        public bool Check(string userName)
        {
            if (!_userRoutes.ContainsKey(userName) || !_stack.ContainsKey(userName))
                return true;

            ClearQueue(userName);

            foreach (var routeLimit in _userRoutes[userName]) // si une seule des règles est enfreinte, return false
            {
                if (_stack[userName].Where(r => r.Route.Equals(routeLimit.Key)).Count() > routeLimit.Value)
                    return false;
            }

            return true;
        }

        private void ClearQueue(string userName)
        {
            if (!_stack.ContainsKey(userName))
                return;

            var item = _stack[userName].FirstOrDefault();
            var limitTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _queueLivingMs;
            while (item != null && item.TimeStamp < limitTimeStamp)
            {
                _stack[userName].TryDequeue(out item);
                item = _stack[userName].FirstOrDefault();
            }
        }

        class QueueElem
        {
            public long TimeStamp { get; set; }
            public string Route { get; set; }

            public QueueElem(long timeStamp, string route)
            {
                this.TimeStamp = timeStamp;
                this.Route = route;
            }
        }
    }
}
