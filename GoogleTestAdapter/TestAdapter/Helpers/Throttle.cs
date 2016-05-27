using System;
using System.Collections.Generic;
using System.Threading;

namespace GoogleTestAdapter.TestAdapter.Helpers
{
    public class Throttle
    {
        private readonly int _maxEventsPerTimeSpan;
        private readonly TimeSpan _timeSpan;
        private readonly Queue<DateTime> _eventTimesHistory;

        public Throttle(int maxEventsPerTimeSpan, TimeSpan timeSpan)
        {
            _maxEventsPerTimeSpan = maxEventsPerTimeSpan;
            _timeSpan = timeSpan;
            _eventTimesHistory = new Queue<DateTime>(maxEventsPerTimeSpan);
        }

        public void Execute(Action task)
        {
            if (_eventTimesHistory.Count == _maxEventsPerTimeSpan)
            {
                var otherEventTime = _eventTimesHistory.Dequeue();
                var timePassed = DateTime.Now - otherEventTime;
                if (timePassed < _timeSpan)
                    Thread.Sleep(_timeSpan - timePassed);
            }

            task.Invoke();
            _eventTimesHistory.Enqueue(DateTime.Now);
        }
    }
}
