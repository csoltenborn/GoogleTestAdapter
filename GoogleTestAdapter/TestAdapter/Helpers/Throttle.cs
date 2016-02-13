using System;
using System.Collections.Generic;
using System.Threading;

namespace GoogleTestAdapter.TestAdapter.Helpers
{
    public class Throttle
    {
        private int maxEventsPerTimeSpan;
        private TimeSpan timeSpan;
        private Queue<DateTime> eventTimesHistory;

        public Throttle(int maxEventsPerTimeSpan, TimeSpan timeSpan)
        {
            this.maxEventsPerTimeSpan = maxEventsPerTimeSpan;
            this.timeSpan = timeSpan;
            eventTimesHistory = new Queue<DateTime>(maxEventsPerTimeSpan);
        }

        public void Execute(Action task)
        {
            if (eventTimesHistory.Count == maxEventsPerTimeSpan)
            {
                var otherEventTime = eventTimesHistory.Dequeue();
                var timePassed = DateTime.Now - otherEventTime;
                if (timePassed < timeSpan)
                    Thread.Sleep(timeSpan - timePassed);
            }

            task.Invoke();
            eventTimesHistory.Enqueue(DateTime.Now);
        }
    }
}
