using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleTestAdapter.VsPackage.Automation
{
    /// <summary>
    /// Test discoveries of the automation object, which run in the background since COM calls arrive on Visual
    /// Studio's UI thread; the results of the latest discoveries are kept until they are retrieved by id.
    /// </summary>
    public class TestDiscoveryJobs
    {
        public const string Running = "Running";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
        public const string Unknown = "Unknown";

        public const int MaxNrOfJobs = 32;

        public class Job
        {
            public string Id { get; } = Guid.NewGuid().ToString();
            public string Executable { get; }
            public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();
            public volatile string Status = Running;
            public CollectingLogger Logger { get; } = new CollectingLogger();
            // set before Status changes to Completed
            public List<JsonObject> Tests { get; set; }

            public Job(string executable)
            {
                Executable = executable;
            }
        }

        private readonly List<Job> _jobs = new List<Job>();

        /// <param name="discover">discovers the tests of the executable, and returns whether it has succeeded</param>
        public Job Start(string executable, Func<Job, bool> discover)
        {
            var job = new Job(executable);
            lock (_jobs)
            {
                _jobs.Add(job);
                // the oldest finished jobs are dropped first
                while (_jobs.Count > MaxNrOfJobs)
                {
                    Job oldest = _jobs.FirstOrDefault(j => j.Status != Running) ?? _jobs[0];
                    _jobs.Remove(oldest);
                }
            }

            Task.Factory.StartNew(() =>
            {
                bool succeeded = false;
                try
                {
                    succeeded = discover(job);
                }
                finally
                {
                    job.Stopwatch.Stop();
                    job.Status = succeeded ? Completed : Failed;
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return job;
        }

        public Job Get(string id)
        {
            lock (_jobs)
            {
                return _jobs.FirstOrDefault(j => j.Id == id);
            }
        }
    }
}
