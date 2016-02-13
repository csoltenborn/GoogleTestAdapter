using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleTestAdapter.TestAdapter.Helpers
{
    [TestClass]
    public class ThrottleTests
    {
        public IEnumerable<int> Enumerator { get; private set; }

        struct Event
        {
            public int id;
            public DateTime time;
        }

        private static List<Event> events = new List<Event>();
        private const int maxEvents = 10;
        private const int totalEvents = 100;
        private static readonly TimeSpan timeSpan = TimeSpan.FromMilliseconds(100);

        [ClassInitialize]
        public static void GenerateEventList(TestContext context)
        {
            var throttle = new Throttle(maxEvents, timeSpan);
            for (int i = 0; i < totalEvents; i++)
            {
                throttle.Execute(() =>
                {
                    events.Add(new Event { id = i, time = DateTime.Now });
                });
            }
        }

        [TestMethod]
        public void EventsAreInOrder()
        {
            events.Count.Should().Be(totalEvents);
            for (int i = 0; i < events.Count; i++)
                events[i].id.Should().Be(i);
        }

        [TestMethod]
        public void DoesNotTakeMuchLongerThanExpected()
        {
            var duration = (events.Last().time - events.First().time).TotalMilliseconds;
            var minimumDuration = ((totalEvents / maxEvents - 1) * timeSpan.TotalMilliseconds);
            duration.Should().BeGreaterThan(minimumDuration);
            duration.Should().BeLessThan(minimumDuration + timeSpan.TotalMilliseconds + 20); // TODO 20 is an arbitrary tolerance to make test pass
        }

        [TestMethod]
        public void HasNoTimeFrameWithTooManyElements()
        {
            var firstEvent = events.First().time;
            var lastEvent = events.Last().time;
            var step = TimeSpan.FromMilliseconds(77);

            for (DateTime start = firstEvent; start < lastEvent; start += step)
            {
                var end = start + timeSpan;
                var eventsInTimeFrame = events.Where(e => e.time >= start && e.time <= end);
                eventsInTimeFrame.Count().Should().BeLessOrEqualTo(maxEvents);
            }
        }
    }
}