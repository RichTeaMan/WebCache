using System;
using System.Collections.Concurrent;
using System.Linq;

namespace RichTea.WebCache
{
    public class RateLimit
    {
        private readonly object dequeueLock = new object();
        private ConcurrentQueue<long> requestTimes = new ConcurrentQueue<long>();

        /// <summary>
        /// Gets or sets interval for rate limiting, in seconds.
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// Gets ors sets the amount of requests allowed.
        /// </summary>
        public int Requests { get; set; }

        public void AddRequest()
        {
            long seconds = (long)TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
            requestTimes.Enqueue(seconds);
        }

        public bool IsThrottled()
        {
            long seconds = (long)TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
            long cutoff = seconds - (2 * Interval);

            var recentRequests = requestTimes.ToArray().Count(t => t > cutoff);
            bool throttled = recentRequests + 10 > Requests;

            if (!requestTimes.IsEmpty && requestTimes.Count % 20 == 0)
            {
                lock (dequeueLock)
                {
                    long removeCutoff = seconds - (2 * Interval);
                    while (true)
                    {
                        long peeked;
                        bool canPeek = requestTimes.TryPeek(out peeked);
                        if (canPeek && peeked < removeCutoff)
                        {
                            requestTimes.TryDequeue(out peeked);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

            }

            return throttled;
        }
    }
}
